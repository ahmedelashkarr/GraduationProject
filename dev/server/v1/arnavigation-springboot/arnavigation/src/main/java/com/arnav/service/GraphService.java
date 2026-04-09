package com.arnav.service;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.io.File;
import java.io.IOException;
import java.util.*;


@Service
public class GraphService {

    private static final Logger log = LoggerFactory.getLogger(GraphService.class);

    @Value("${app.data.building-graph-path}")
    private String graphPath;

    private Map<String, Map<String, Object>> nodes = new HashMap<>();
    private List<Map<String, Object>> edges       = new ArrayList<>();

    @PostConstruct
    public void load() throws IOException {
        File file = new File(graphPath);
        if (!file.exists()) {
            log.warn("Building graph not found at {}. Navigation will be unavailable.", graphPath);
            return;
        }
        ObjectMapper mapper = new ObjectMapper();
        Map<String, Object> raw = mapper.readValue(file, new TypeReference<>() {});

        @SuppressWarnings("unchecked")
        Map<String, Map<String, Object>> n = (Map<String, Map<String, Object>>) raw.get("nodes");
        if (n != null) nodes = n;

        @SuppressWarnings("unchecked")
        List<Map<String, Object>> e = (List<Map<String, Object>>) raw.get("edges");
        if (e != null) edges = e;

        log.info("Graph loaded: {} nodes, {} edges", nodes.size(), edges.size());
    }

    public boolean nodeExists(String zoneId) {
        return nodes.containsKey(zoneId);
    }

    public Map<String, Object> getNode(String zoneId) {
        return nodes.get(zoneId);
    }

    /**
     * Returns an unmodifiable view of all nodes.
     * Used by FusionService to find the nearest zone to an (x, y) coordinate.
     */
    public Map<String, Map<String, Object>> getAllNodes() {
        return Collections.unmodifiableMap(nodes);
    }

    /**
     * Returns list of (neighborZone, weight, directionLabel) triples.
     */
    public List<Neighbor> neighbors(String zoneId) {
        List<Neighbor> result = new ArrayList<>();
        for (Map<String, Object> edge : edges) {
            String from      = (String)  edge.get("from");
            String to        = (String)  edge.get("to");
            double weight    = toDouble(edge.getOrDefault("weight", 1.0));
            String direction = (String)  edge.getOrDefault("direction", "");
            boolean directed = Boolean.TRUE.equals(edge.get("directed"));

            if (zoneId.equals(from)) {
                result.add(new Neighbor(to, weight, direction));
            } else if (!directed && zoneId.equals(to)) {
                result.add(new Neighbor(from, weight, direction));
            }
        }
        return result;
    }

    private static double toDouble(Object v) {
        if (v instanceof Number n) return n.doubleValue();
        return 1.0;
    }

    public record Neighbor(String zoneId, double weight, String direction) {}
}
