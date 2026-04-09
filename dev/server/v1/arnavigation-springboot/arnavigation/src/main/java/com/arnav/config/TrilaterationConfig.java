package com.arnav.config;

import com.arnav.service.TrilaterationService;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.io.File;
import java.io.IOException;
import java.util.HashMap;
import java.util.Map;

/**
 * BUG FIX: This entire class was missing from the project.
 *
 * TrilaterationService declares a constructor that takes
 *   Map<String, TrilaterationService.AccessPoint>
 * but no @Bean of that type existed anywhere → Spring Boot crashed at startup
 * with "No qualifying bean of type Map".
 *
 * This config reads ap_map.json and produces the required bean.
 *
 * Expected ap_map.json structure:
 * {
 *   "AA:BB:CC:DD:EE:FF": { "x": 10.5, "y": 3.2, "tx_power": -59 },
 *   ...
 * }
 */
@Configuration
public class TrilaterationConfig {

    private static final Logger log = LoggerFactory.getLogger(TrilaterationConfig.class);

    @Value("${app.data.ap-map-path}")
    private String apMapPath;

    @Bean
    public Map<String, TrilaterationService.AccessPoint> apMap() {
        Map<String, TrilaterationService.AccessPoint> result = new HashMap<>();

        File file = new File(apMapPath);
        if (!file.exists()) {
            log.warn("AP map not found at {}. Trilateration will be disabled.", apMapPath);
            return result;
        }

        try {
            ObjectMapper mapper = new ObjectMapper();
            // Raw map: bssid -> { "x": ..., "y": ..., "tx_power": ... }
            Map<String, Map<String, Object>> raw =
                    mapper.readValue(file, new TypeReference<>() {});

            raw.forEach((bssid, props) -> {
                double x       = toDouble(props.get("x"));
                double y       = toDouble(props.get("y"));
                int    txPower = toInt(props.getOrDefault("tx_power", -59));
                result.put(bssid, new TrilaterationService.AccessPoint(x, y, txPower));
            });

            log.info("AP map loaded: {} access points", result.size());
        } catch (IOException e) {
            log.error("Failed to load AP map from {}: {}", apMapPath, e.getMessage());
        }

        return result;
    }

    private static double toDouble(Object v) {
        if (v instanceof Number n) return n.doubleValue();
        return 0.0;
    }

    private static int toInt(Object v) {
        if (v instanceof Number n) return n.intValue();
        return -59;
    }
}
