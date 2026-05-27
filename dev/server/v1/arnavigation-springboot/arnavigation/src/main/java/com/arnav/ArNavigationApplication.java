package com.arnav;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import com.arnav.config.AppConfig;

// BUG FIX: @EnableConfigurationProperties is required so that Spring Boot
// binds @ConfigurationProperties beans (AppConfig) at startup.
// Without it, AppConfig fields remain null (dir, fingerprintsPath, etc.)
// causing NullPointerExceptions when services try to read data files.
@SpringBootApplication
public class ArNavigationApplication {

    public static void main(String[] args) {
        SpringApplication.run(ArNavigationApplication.class, args);
    }
}
