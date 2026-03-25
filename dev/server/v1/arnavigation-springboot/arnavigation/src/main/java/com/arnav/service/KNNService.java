package com.arnav.service;

import com.arnav.config.AppConfig;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.io.File;
import java.io.IOException;
import java.util.*;

/**
 * Predicts the current zone from a live WiFi scan using K-Nearest Neighbors.
 * Direct port of services/knn_service.py.
 *
 * fingerprints.json structure:
 * [ { "zone": "F1_LOBBY", "signals": { "AA:BB:CC:DD:EE:FF": -65, ... } }, ... ]
 */
@Service
public class KNNService {

    private static final Logger log = LoggerFactory.getLogger(KNNService.class);

    private final AppConfig config;
    private List<Fingerprint> fingerprints = List.of();

    public KNNService(AppConfig config) {
        this.config = config;
    }

    /** Loads fingerprint DB on first call (lazy, thread-safe enough for single-deploy). */
    private synchronized List<Fingerprint> getFingerprints() {
        if (!fingerprints.isEmpty()) return fingerprints;
        String path = config.getData().getFingerprintsPath();
        File file = new File(path);
        if (!file.exists()) {
            log.warn("Fingerprints not found at {}", path);
            return fingerprints;
        }
        try {
            ObjectMapper mapper = new ObjectMapper();
            fingerprints = mapper.readValue(file, new TypeReference<>() {});
            log.info("Loaded {} fingerprints", fingerprints.size());
        } catch (IOException e) {
            log.error("Failed to load fingerprints", e);
        }
        return fingerprints;
    }

    /**
     * @param scan map of BSSID → RSSI
     * @return PredictionResult with zoneId and confidence [0,1]
     */
    public PredictionResult predict(Map<String, Integer> scan) {
        List<Fingerprint> fps = getFingerprints();
        if (fps.isEmpty()) throw new IllegalStateException("Fingerprint database is empty");

        int k            = config.getKnn().getK();
        int rssiMissing  = config.getKnn().getRssiMissing();

        // Compute distances to all fingerprints
        List<double[]> distances = new ArrayList<>(); // [dist, index]
        for (int i = 0; i < fps.size(); i++) {
            double dist = euclidean(scan, fps.get(i).signals(), rssiMissing);
            distances.add(new double[]{dist, i});
        }
        distances.sort(Comparator.comparingDouble(a -> a[0]));

        List<double[]> neighbors = distances.subList(0, Math.min(k, distances.size()));

        // Weighted vote: weight = 1 / (dist + ε)
        Map<String, Double> votes = new HashMap<>();
        for (double[] nd : neighbors) {
            String zone  = fps.get((int) nd[1]).zone();
            double weight = 1.0 / (nd[0] + 1e-6);
            votes.merge(zone, weight, Double::sum);
        }

        double total    = votes.values().stream().mapToDouble(Double::doubleValue).sum();
        String bestZone = Collections.max(votes.entrySet(), Map.Entry.comparingByValue()).getKey();
        double confidence = total > 0 ? votes.get(bestZone) / total : 0.0;

        return new PredictionResult(bestZone, confidence);
    }

    private static double euclidean(Map<String, Integer> scan,
                                    Map<String, Integer> fpSignals,
                                    int rssiMissing) {
        Set<String> allBssids = new HashSet<>(scan.keySet());
        allBssids.addAll(fpSignals.keySet());
        double sqSum = 0.0;
        for (String bssid : allBssids) {
            int a = scan.getOrDefault(bssid, rssiMissing);
            int b = fpSignals.getOrDefault(bssid, rssiMissing);
            sqSum += (double)(a - b) * (a - b);
        }
        return Math.sqrt(sqSum);
    }

    public record PredictionResult(String zone, double confidence) {}

    public record Fingerprint(String zone, Map<String, Integer> signals) {}
}
