package com.arnav.controller;

import com.arnav.service.RoomsService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.io.IOException;
import java.util.List;
import java.util.Map;

/**
 * GET /rooms?q=&lt;search&gt;&amp;floor=&lt;int&gt;&amp;type=&lt;type&gt;
 * Returns: { "rooms": [...], "count": int }
 */
@RestController
public class RoomsController {

    private static final Logger log = LoggerFactory.getLogger(RoomsController.class);

    private final RoomsService roomsService;

    public RoomsController(RoomsService roomsService) {
        this.roomsService = roomsService;
    }

    @GetMapping("/rooms")
    public ResponseEntity<?> getRooms(
            @RequestParam(name = "q",     required = false) String query,
            @RequestParam(name = "floor", required = false) Integer floor,
            @RequestParam(name = "type",  required = false) String type) {

        try {
            List<Map<String, Object>> rooms = roomsService.query(query, floor, type);
            return ResponseEntity.ok(Map.of("rooms", rooms, "count", rooms.size()));
        } catch (IOException e) {
            if (e.getMessage() != null && e.getMessage().contains("not found")) {
                return ResponseEntity.internalServerError()
                        .body(Map.of("error", "rooms.json not found"));
            }
            log.error("Rooms error", e);
            return ResponseEntity.internalServerError().body(Map.of("error", e.getMessage()));
        } catch (Exception e) {
            log.error("Rooms error", e);
            return ResponseEntity.internalServerError().body(Map.of("error", e.getMessage()));
        }
    }

}
