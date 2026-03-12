import pandas as pd
import numpy as np
import matplotlib.pyplot as plt

# ==============================
#  Load training data
# ==============================
file_name = "training.xlsx"
df = pd.read_excel(file_name)

print("Training data loaded:")
print(df.head(), "\n")

# ==============================
#  Compute fingerprint (mean RSSI per point)
# ==============================
fingerprint = df.groupby('point_id')['rssi'].mean()
coords = df.groupby('point_id')[['x', 'y']].first()


# ==============================
#  KNN Localization Function
# ==============================
def predict_location_knn(rssi_value, k=3):
    # احسب الفرق بين قراءة الـ RSSI الحالية والفينجر برنت
    diffs = abs(fingerprint - rssi_value)

    # خُد أقرب K نقاط
    nearest = diffs.nsmallest(k)

    # أسماء النقاط المختارة
    selected_points = nearest.index

    # المسافات (الفرق بين RSSI)
    distances = nearest.values

    # الوزن: كلما اقتربت القيمة زاد الوزن (1 / الفرق)
    weights = 1 / (distances + 0.001)

    # الحساب المرجّح Weighted Position
    x_pred = np.sum(weights * coords.loc[selected_points]['x']) / np.sum(weights)
    y_pred = np.sum(weights * coords.loc[selected_points]['y']) / np.sum(weights)

    # أقرب نقطة fingerprint (اسمياً)
    best_point = selected_points[0]

    return best_point, (x_pred, y_pred), nearest


# ==============================
#  Plotting Function
# ==============================
def plot_prediction(pred_real_point, pred_xy):
    plt.figure(figsize=(6, 6))
    plt.grid(True, linestyle='--', alpha=0.4)
    plt.title("Indoor Bluetooth Localization (KNN)", fontsize=14)

    # نقاط الخريطة
    for point, row in coords.iterrows():
        plt.scatter(row.x, row.y, c='gray', s=120)
        plt.text(row.x + 0.05, row.y + 0.05, point, fontsize=12)

    # النقطة التنبؤية (موقع الشخص)
    px, py = pred_xy
    plt.scatter(px, py, c='red', s=200, label="Estimated Position")

    # fingerprint الأقرب (للمقارنة فقط)
    rx, ry = coords.loc[pred_real_point]
    plt.scatter(rx, ry, c='blue', s=150, label=f"Closest FP: {pred_real_point}")

    plt.xlabel("X Position")
    plt.ylabel("Y Position")
    plt.legend()
    plt.show()


# ==============================
#  Main Loop
# ==============================
while True:
    val = input("Enter RSSI reading (or 'exit'): ").strip()

    if val.lower() in ("exit", "quit"):
        print("Program terminated.")
        break

    try:
        rssi_val = float(val)

        best_point, (x_est, y_est), nearest = predict_location_knn(rssi_val)

        print("\n======================================")
        print(f"Closest Fingerprint Point = {best_point}")
        print(f"Estimated Position = ({x_est:.2f}, {y_est:.2f})")
        print("Nearest matches:")
        print(nearest)
        print("======================================\n")

        plot_prediction(best_point, (x_est, y_est))

    except Exception as e:
        print("Error:", e)
