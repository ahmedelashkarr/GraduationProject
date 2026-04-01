package com.arnav.controller;

import com.arnav.config.AppConfig;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.core.io.FileSystemResource;
import org.springframework.core.io.Resource;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.io.File;
import java.util.Map;

/**
 * GET /floorplan/{floor}
 * Returns the PNG image for the requested floor.
 */
@RestController
public class FloorplanController {

    private static final Logger log = LoggerFactory.getLogger(FloorplanController.class);

    private final AppConfig config;

    public FloorplanController(AppConfig config) {
        this.config = config;
    }

    @GetMapping("/floorplan/{floor}")
    public ResponseEntity<?> getFloorplan(@PathVariable int floor) {
        String dir      = config.getData().getFloorplansDir();
        String filename = "floor_" + floor + ".png";
        File   file     = new File(dir, filename);

        if (!file.isFile()) {
            return ResponseEntity.status(404)
                    .body(Map.of("error", "Floor plan for floor " + floor + " not found"));
        }

        Resource resource = new FileSystemResource(file);
        return ResponseEntity.ok()
                .contentType(MediaType.IMAGE_PNG)
                .body(resource);
    }
}
