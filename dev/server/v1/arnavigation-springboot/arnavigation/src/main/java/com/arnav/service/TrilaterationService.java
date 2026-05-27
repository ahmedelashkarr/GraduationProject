package com.arnav.service;

import org.apache.commons.math3.optim.InitialGuess;
import org.apache.commons.math3.optim.MaxEval;
import org.apache.commons.math3.optim.PointValuePair;
import org.apache.commons.math3.optim.nonlinear.scalar.GoalType;
import org.apache.commons.math3.optim.nonlinear.scalar.ObjectiveFunction;
import org.apache.commons.math3.optim.nonlinear.scalar.noderiv.NelderMeadSimplex;
import org.apache.commons.math3.optim.nonlinear.scalar.noderiv.SimplexOptimizer;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

/**
 * Estimates (x, y) position from a WiFi scan using trilateration.
 *
 * BUG FIX: The original code had no @Bean producing Map<String, AccessPoint>,
 * so Spring could not inject this constructor dependency.
 * Fixed by adding TrilaterationConfig (new file) which reads ap_map.json
 * and produces that bean.
 *
 * BUG FIX: The constructor parameter type was
 *   Map<String, AccessPoint>
 * but Spring's raw-type erasure can match any Map bean unless the generic
 * is a concrete class registered as a bean. TrilaterationConfig now
 * explicitly declares the correct return type so the injection is unambiguous.
 */
@Service
public class TrilaterationService {

    private static final Logger log = LoggerFactory.getLogger(TrilaterationService.class);

    /** Path-loss exponent – tune for your building (Python default: 2.7). */
    private static final double PATH_LOSS_N = 2.7;

    private final Map<String, AccessPoint> apMap;

    public TrilaterationService(Map<String, AccessPoint> apMap) {
        this.apMap = apMap;
    }

    // ── public ───────────────────────────────────────────────────────────────

    /**
     * Converts RSSI to an estimated distance in metres.
     */
    public double rssiToDistance(int rssi, int txPower) {
        return Math.pow(10.0, (txPower - rssi) / (10.0 * PATH_LOSS_N));
    }

    /**
     * Estimates the (x, y) position from a live WiFi scan.
     *
     * @param scan  map of BSSID → RSSI
     * @return Optional containing the estimated position, or empty if fewer than 3 APs visible.
     */
    public Optional<Position> estimatePosition(Map<String, Integer> scan) {
        List<double[]> points    = new ArrayList<>();
        List<Double>   distances = new ArrayList<>();

        for (Map.Entry<String, Integer> entry : scan.entrySet()) {
            AccessPoint ap = apMap.get(entry.getKey());
            if (ap == null) continue;
            double d = rssiToDistance(entry.getValue(), ap.txPower());
            points.add(new double[]{ap.x(), ap.y()});
            distances.add(d);
        }

        if (points.size() < 3) {
            log.debug("Trilateration skipped – only {} AP(s) visible (need ≥3)", points.size());
            return Optional.empty();
        }

        double x0 = points.stream().mapToDouble(p -> p[0]).average().orElse(0);
        double y0 = points.stream().mapToDouble(p -> p[1]).average().orElse(0);

        SimplexOptimizer optimizer = new SimplexOptimizer(1e-6, 1e-10);

        final List<double[]> pts = points;
        final List<Double>   dst = distances;

        try {
            PointValuePair result = optimizer.optimize(
                    new MaxEval(10_000),
                    new ObjectiveFunction(pos -> {
                        double err = 0;
                        for (int i = 0; i < pts.size(); i++) {
                            double dx   = pos[0] - pts.get(i)[0];
                            double dy   = pos[1] - pts.get(i)[1];
                            double diff = Math.sqrt(dx * dx + dy * dy) - dst.get(i);
                            err += diff * diff;
                        }
                        return err;
                    }),
                    GoalType.MINIMIZE,
                    new InitialGuess(new double[]{x0, y0}),
                    new NelderMeadSimplex(2)
            );

            double[] xy = result.getPoint();
            return Optional.of(new Position(
                    Math.round(xy[0] * 100.0) / 100.0,
                    Math.round(xy[1] * 100.0) / 100.0
            ));

        } catch (Exception e) {
            log.warn("Trilateration optimisation failed: {}", e.getMessage());
            return Optional.empty();
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    public record AccessPoint(double x, double y, int txPower) {}

    public record Position(double x, double y) {}
}
