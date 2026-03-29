package com.arnav.service;

import org.springframework.stereotype.Component;

/**
 * 1-D Kalman filter to smooth a scalar signal (e.g. zone confidence).
 * Direct port of services/localization.py – KalmanFilter.
 */
@Component
public class KalmanFilter {

    private final double Q; // process variance
    private final double R; // measurement variance

    private Double x = null; // state estimate
    private double P = 1.0;  // estimation error covariance

    public KalmanFilter() {
        this(1e-3, 0.1);
    }

    public KalmanFilter(double processNoise, double measurementNoise) {
        this.Q = processNoise;
        this.R = measurementNoise;
    }

    public synchronized double update(double measurement) {
        if (x == null) {
            x = measurement;
            return x;
        }
        // Predict
        double pPred = P + Q;
        // Kalman gain
        double K = pPred / (pPred + R);
        x = x + K * (measurement - x);
        P = (1 - K) * pPred;
        return x;
    }
}
