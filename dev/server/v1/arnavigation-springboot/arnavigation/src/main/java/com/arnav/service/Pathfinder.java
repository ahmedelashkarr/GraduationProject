package com.arnav.service;

import org.springframework.stereotype.Service;

import java.util.*;

/**
 * Dijkstra pathfinder (A* with zero heuristic) over the building graph.
 * Direct port of services/pathfinder.py.
 */
@Service
public class Pathfinder {

    private final GraphService graph;

    public Pathfinder(GraphService graph) {
        this.graph = graph;
    }

    /**
     * Finds the shortest path from origin to destination.
     *
     * @return PathResult with steps and total distance, or empty if unreachable.
     * @throws IllegalArgumentException if a zone is not found in the graph.
     */
    public Optional<PathResult> findPath(String origin, String destination) {
        for (String zone : List.of(origin, destination)) {
            if (!graph.nodeExists(zone)) {
                throw new IllegalArgumentException("Zone '" + zone + "' not found in building graph");
            }
        }

        // heap entry: [cost, zone, path, lastDirection]
        // Using a PriorityQueue sorted by cost
        record HeapEntry(double cost, String zone, List<PathNode> path, String lastDir)
                implements Comparable<HeapEntry> {
            public int compareTo(HeapEntry o) { return Double.compare(this.cost, o.cost); }
        }

        PriorityQueue<HeapEntry> heap = new PriorityQueue<>();
        heap.add(new HeapEntry(0.0, origin, List.of(), ""));

        Map<String, Double> visited = new HashMap<>();

        while (!heap.isEmpty()) {
            HeapEntry entry = heap.poll();
            double cost    = entry.cost();
            String current = entry.zone();

            if (visited.containsKey(current) && visited.get(current) <= cost) continue;
            visited.put(current, cost);

            List<PathNode> newPath = new ArrayList<>(entry.path());
            newPath.add(new PathNode(current, entry.lastDir(), cost));

            if (current.equals(destination)) {
                return Optional.of(new PathResult(buildSteps(newPath), Math.round(cost * 100.0) / 100.0));
            }

            for (GraphService.Neighbor nb : graph.neighbors(current)) {
                double newCost = cost + nb.weight();
                if (!visited.containsKey(nb.zoneId()) || visited.get(nb.zoneId()) > newCost) {
                    heap.add(new HeapEntry(newCost, nb.zoneId(), newPath, nb.direction()));
                }
            }
        }

        return Optional.empty(); // no path
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private List<Step> buildSteps(List<PathNode> path) {
        List<Step> steps = new ArrayList<>();
        for (int i = 1; i < path.size(); i++) {
            PathNode cur  = path.get(i);
            PathNode prev = path.get(i - 1);
            double segDist = Math.round((cur.cumulativeDist() - prev.cumulativeDist()) * 100.0) / 100.0;
            String instruction = cur.direction() != null && !cur.direction().isBlank()
                    ? cur.direction() + " – head to " + cur.zone()
                    : "Proceed to " + cur.zone();
            steps.add(new Step(cur.zone(), cur.direction(), segDist, instruction));
        }
        return steps;
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    record PathNode(String zone, String direction, double cumulativeDist) {}

    public record Step(String zone, String direction, double distance, String instruction) {}

    public record PathResult(List<Step> steps, double totalDistance) {}
}
