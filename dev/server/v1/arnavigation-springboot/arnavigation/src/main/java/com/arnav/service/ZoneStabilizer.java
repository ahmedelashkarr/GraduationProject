package com.arnav.service;

import org.springframework.stereotype.Component;

import java.util.ArrayDeque;
import java.util.Deque;
import java.util.HashMap;
import java.util.Map;

/**
 * Smooths noisy KNN predictions by requiring a zone to win a
 * sliding-window majority before committing to it.
 * Direct port of services/localization.py – ZoneStabilizer.
 *
 * NOTE: A single shared instance is injected into LocateController.
 * For multi-user scenarios consider a per-session stabilizer map.
 */
@Component
public class ZoneStabilizer {

    private final Deque<String> history = new ArrayDeque<>();
    private String currentZone = null;

    /**
     * @param rawZone     raw zone from KNN
     * @param confidence  KNN confidence (unused in vote logic but kept for API parity)
     * @param historySize sliding window size
     * @param threshold   fraction needed to commit (e.g. 0.6 = 60 %)
     * @return stabilised zone id
     */
    public synchronized String update(String rawZone,
                                      double confidence,
                                      int historySize,
                                      double threshold) {
        history.addLast(rawZone);
        if (history.size() > historySize) {
            history.removeFirst();
        }

        Map<String, Integer> votes = new HashMap<>();
        for (String z : history) {
            votes.merge(z, 1, Integer::sum);
        }

        String best = votes.entrySet().stream()
                .max(Map.Entry.comparingByValue())
                .map(Map.Entry::getKey)
                .orElse(rawZone);

        double ratio = (double) votes.get(best) / history.size();

        if (ratio >= threshold) {
            currentZone = best;
        }

        return currentZone != null ? currentZone : rawZone;
    }
}
