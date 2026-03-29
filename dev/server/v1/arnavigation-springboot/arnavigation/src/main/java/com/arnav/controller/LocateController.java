package com.arnav.controller;

import com.arnav.config.AppConfig;
import com.arnav.service.KNNService;
import com.arnav.service.ZoneStabilizer;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

/**
 * POST /locate
 * Body: { "scan": { "BSSID1": rssi, "BSSID2": rssi, ... } }
 * Returns: { "zone": str, "floor": int, "confidence": float }
 */
@RestController
public class LocateController {

    private static final Logger log = LoggerFactory.getLogger(LocateController.class);

    private final KNNService knnService;
    private final ZoneStabilizer stabilizer;
    private final AppConfig config;

    public LocateController(KNNService knnService,
                            ZoneStabilizer stabilizer,
                            AppConfig config) {
        this.knnService = knnService;
        this.stabilizer  = stabilizer;
        this.config       = config;
    }

    @PostMapping("/locate")
    public ResponseEntity<?> locate(@RequestBody(required = false) Map<String, Object> body) {
        if (body == null || !body.containsKey("scan")) {
            return ResponseEntity.badRequest().body(Map.of("error", "Missing 'scan' in request body"));
        }

        Object scanObj = body.get("scan");
        if (!(scanObj instanceof Map)) {
            return ResponseEntity.badRequest()
                    .body(Map.of("error", "'scan' must be an object mapping BSSID→RSSI"));
        }

        try {
            @SuppressWarnings("unchecked")
            Map<String, Object> rawScan = (Map<String, Object>) scanObj;


            Map<String, Integer> scan = new java.util.HashMap<>();
            rawScan.forEach((k, v) -> scan.put(k, ((Number) v).intValue()));

            KNNService.PredictionResult pred = knnService.predict(scan);

            AppConfig.Zone zoneCfg = config.getZone();
            String stableZone = stabilizer.update(
                    pred.zone(), pred.confidence(),
                    zoneCfg.getHistorySize(), zoneCfg.getConfidenceThreshold()
            );


            int floor = 1;
            if (stableZone.contains("_")) {
                try {
                    floor = Integer.parseInt(stableZone.split("_")[0].replace("F", ""));
                } catch (NumberFormatException ignored) {}
            }

            double conf = Math.round(pred.confidence() * 1000.0) / 1000.0;
            return ResponseEntity.ok(Map.of("zone", stableZone, "floor", floor, "confidence", conf));

        } catch (Exception e) {
            log.error("Locate error", e);
            return ResponseEntity.internalServerError().body(Map.of("error", e.getMessage()));
        }
    }
}
