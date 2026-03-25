package com.arnav;

import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.TestPropertySource;

@SpringBootTest
@TestPropertySource(properties = {
        "app.data.fingerprints-path=src/test/resources/fingerprints.json",
        "app.data.building-graph-path=src/test/resources/building_graph.json",
        "app.data.rooms-path=src/test/resources/rooms.json",
        "app.data.floorplans-dir=src/test/resources/floorplans"
})
class ArNavigationApplicationTests {

    @Test
    void contextLoads() {
        // Verifies the Spring context starts without errors
    }
}
