package com.arnav.config;

import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

@Configuration
@ConfigurationProperties(prefix = "app")
@Getter
@Setter
public class AppConfig {

    private Knn knn = new Knn();
    private Zone zone = new Zone();
    private Data data = new Data();

    @Getter @Setter
    public static class Knn {
        private int k = 3;
        private int rssiMissing = -100;
    }

    @Getter @Setter
    public static class Zone {
        private int historySize = 5;
        private double confidenceThreshold = 0.6;
    }

    @Getter @Setter
    public static class Data {
        private String dir;
        private String fingerprintsPath;
        private String buildingGraphPath;
        private String roomsPath;
        private String floorplansDir;
        // BUG FIX: added apMapPath field - it was missing, causing @Value injection
        // in TrilaterationConfig to have no matching AppConfig field to bind from.
        private String apMapPath;
    }
}
