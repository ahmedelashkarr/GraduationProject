# import pandas as pd
# import numpy as np
# from matplotlib import pyplot as plt
# # ==============================
# # 1️⃣ قراءة بيانات التدريب من ملف Excel
# # ==============================
# file_name = "training.xlsx"
# df = pd.read_excel(file_name)

# # نطبع للتأكد
# print("✅ Loaded training data:")
# print(df.head(), "\n")


# # ==============================
# # 2️⃣ نحسب متوسط RSSI لكل نقطة
# # ==============================
# fingerprint = df.groupby('point_id')['rssi'].mean()
# coords = df.groupby('point_id')[['x', 'y']].first()

# # ==============================
# # 3️⃣ دالة لتوقع الموقع الأقرب
# # ==============================
# def predict_location(rssi_value):
#     diffs = abs(fingerprint - rssi_value)
#     best_match = diffs.idxmin()
#     best_score = diffs.min()
#     x, y = coords.loc[best_match]
#     return best_match, best_score, (x, y)

# # ==============================
# # 4️⃣ دالة لرسم الخريطة والموقع المتوقع
# # ==============================
# def plot_location(pred_point):
#     plt.figure(figsize=(6,6))
#     plt.grid(True, linestyle='--', alpha=0.4)
#     plt.title("📍 Indoor Bluetooth Localization", fontsize=14)

#     # نرسم كل النقاط
#     for point, row in coords.iterrows():
#         plt.scatter(row.x, row.y, c='gray', s=100)
#         plt.text(row.x+0.05, row.y+0.05, point, fontsize=12)

#     # نرسم النقطة المتوقعة باللون الأحمر
#     px, py = coords.loc[pred_point]
#     plt.scatter(px, py, c='red', s=200, label=f"Predicted: {pred_point}")
#     plt.legend()
#     plt.xlabel("X Position")
#     plt.ylabel("Y Position")
#     plt.show()

# while True:
#     try:
#         val = input("ادخل قراءة RSSI الحالية   ").strip()
#         if val.lower() in ["exit", "quit"]:
#             print("تم الإنهاء ✅")
#             break
#         rssi_val = float(val)
#         point, diff, (x, y) = predict_location(rssi_val)
#         print(f"\n🔹 الموقع المتوقع: {point} (x={x}, y={y}) — الفرق = {diff:.1f} dBm\n")
#         plot_location(point)
#     except Exception as e:
#         print("⚠️ خطأ:", e)
import pandas as pd
import numpy as np
from matplotlib import pyplot as plt

# ==============================
#  Load training data from Excel file
# ==============================
file_name = "training.xlsx"
df = pd.read_excel(file_name)

# Print to verify
print("Training data loaded:")
print(df.head(), "\n")


# ==============================
#  Compute mean RSSI for each point   نحسب متوسط RSSI لكل نقطة

# ==============================
fingerprint = df.groupby('point_id')['rssi'].mean()
coords = df.groupby('point_id')[['x', 'y']].first()


# ==============================
#  Function to predict the nearest location
# ==============================
def predict_location(rssi_value):
    diffs = abs(fingerprint - rssi_value)
    best_match = diffs.idxmin()
    best_score = diffs.min()
    x, y = coords.loc[best_match]
    return best_match, best_score, (x, y)


# ==============================
#  Function to plot map and predicted location  دالة لرسم الخريطة والموقع المتوقع

# ==============================
def plot_location(pred_point):
    plt.figure(figsize=(6, 6))
    plt.grid(True, linestyle='--', alpha=0.4)
    plt.title("Indoor Bluetooth Localization", fontsize=14)

    # Draw all points
    for point, row in coords.iterrows():
        plt.scatter(row.x, row.y, c='gray', s=100)
        plt.text(row.x + 0.05, row.y + 0.05, point, fontsize=12)

    # Draw predicted point in red
    px, py = coords.loc[pred_point]
    plt.scatter(px, py, c='red', s=200, label=f"Predicted: {pred_point}")
    plt.legend()
    plt.xlabel("X Position")
    plt.ylabel("Y Position")
    plt.show()


while True:
    try:
        val = input("Enter current RSSI reading: ").strip()
        if val.lower() in ["exit", "quit"]:
            print("Program terminated")
            break

        rssi_val = float(val)
        point, diff, (x, y) = predict_location(rssi_val)
        print(f"Predicted location: {point} (x={x}, y={y}) — difference = {diff:.1f} dBm\n")
        plot_location(point)

    except Exception as e:
        print("Error:", e)
