package com.arnav.service;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.io.File;
import java.io.IOException;
import java.util.List;
import java.util.Map;
import java.util.stream.Stream;

/**
 * Loads rooms.json and supports filtered queries.
 *
 * rooms.json structure:
 * [ { "name": "Room 101", "floor": 1, "type": "classroom", "zone": "F1_R101", ... }, ... ]
 */
@Service
public class RoomsService {

    private static final Logger log = LoggerFactory.getLogger(RoomsService.class);

    @Value("${app.data.rooms-path}")
    private String roomsPath;

    private List<Map<String, Object>> cache = null;

    private synchronized List<Map<String, Object>> load() throws IOException {
        if (cache != null) return cache;
        File file = new File(roomsPath);
        if (!file.exists()) throw new IOException("rooms.json not found at " + roomsPath);
        ObjectMapper mapper = new ObjectMapper();
        cache = mapper.readValue(file, new TypeReference<>() {});
        log.info("Loaded {} rooms", cache.size());
        return cache;
    }

    public List<Map<String, Object>> query(String nameQuery,
                                           Integer floorFilter,
                                           String typeFilter) throws IOException {
        List<Map<String, Object>> rooms = load();
        Stream<Map<String, Object>> stream = rooms.stream();

        if (nameQuery != null && !nameQuery.isBlank()) {
            final String lc = nameQuery.toLowerCase();
            stream = stream.filter(r -> {
                Object name = r.get("name");
                return name != null && name.toString().toLowerCase().contains(lc);
            });
        }
        if (floorFilter != null) {
            stream = stream.filter(r -> floorFilter.equals(r.get("floor")));
        }
        if (typeFilter != null && !typeFilter.isBlank()) {
            final String lc = typeFilter.toLowerCase();
            stream = stream.filter(r -> {
                Object type = r.get("type");
                return type != null && type.toString().toLowerCase().equals(lc);
            });
        }
        return stream.toList();
    }
}
