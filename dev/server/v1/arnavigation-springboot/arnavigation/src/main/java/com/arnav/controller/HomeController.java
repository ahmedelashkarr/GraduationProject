package com.arnav.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Map;

@RestController
public class HomeController {

    /** GET / */
    @GetMapping("/")
    public ResponseEntity<Map<String, String>> index() {
        return ResponseEntity.ok(Map.of("service", "ARNavigation Server", "version", "1.0"));
    }

    /** GET /health */
    @GetMapping("/health")
    public ResponseEntity<Map<String, String>> health() {
        return ResponseEntity.ok(Map.of("status", "ok"));
    }
}
