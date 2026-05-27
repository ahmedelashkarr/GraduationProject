package com.arnav.controller;

import com.arnav.service.Pathfinder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.Map;
import java.util.Optional;

/**
 * GET /route?from=&lt;zone_id&gt;&amp;to=&lt;zone_id&gt;
 * Returns: { "steps": [...], "total_distance": float }
 */
@RestController
public class RouteController {

    private static final Logger log = LoggerFactory.getLogger(RouteController.class);

    private final Pathfinder pathfinder;

    public RouteController(Pathfinder pathfinder) {
        this.pathfinder = pathfinder;
    }

    @GetMapping("/route")
    public ResponseEntity<?> getRoute(
            @RequestParam(name = "from", defaultValue = "") String from,
            @RequestParam(name = "to",   defaultValue = "") String to) {

        String origin      = from.strip();
        String destination = to.strip();

        if (origin.isEmpty() || destination.isEmpty()) {
            return ResponseEntity.badRequest()
                    .body(Map.of("error", "Query params 'from' and 'to' are required"));
        }

        try {
            Optional<Pathfinder.PathResult> result = pathfinder.findPath(origin, destination);

            if (result.isEmpty()) {
                return ResponseEntity.status(404)
                        .body(Map.of("error",
                                "No path found from '" + origin + "' to '" + destination + "'"));
            }

            Pathfinder.PathResult pr = result.get();
            return ResponseEntity.ok(Map.of(
                    "steps",          pr.steps(),
                    "total_distance", pr.totalDistance()
            ));

        } catch (IllegalArgumentException e) {
            return ResponseEntity.badRequest().body(Map.of("error", e.getMessage()));
        } catch (Exception e) {
            log.error("Route error", e);
            return ResponseEntity.internalServerError().body(Map.of("error", e.getMessage()));
        }
    }
}
