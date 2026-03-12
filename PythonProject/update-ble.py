# # import pandas as pd
# # import numpy as np
# # from matplotlib import pyplot as plt
# # from bleak import BleakScanner
# # import asyncio

# # # ==============================
# # # 1️⃣ Load training data
# # # ==============================
# # file_name = "training.xlsx"
# # df = pd.read_excel(file_name)

# # print("✅ Training data loaded:")
# # print(df.head(), "\n")

# # # ==============================
# # # 2️⃣ Calculate average RSSI per point
# # # ==============================
# # fingerprint = df.groupby('point_id')['rssi'].mean()
# # coords = df.groupby('point_id')[['x', 'y']].first()

# # # ==============================
# # # 3️⃣ Predict location function
# # # ==============================
# # def predict_location(rssi_value):
# #     diffs = abs(fingerprint - rssi_value)
# #     best_match = diffs.idxmin()
# #     best_score = diffs.min()
# #     x, y = coords.loc[best_match]
# #     return best_match, best_score, (x, y)

# # # ==============================
# # # 4️⃣ Plot the map and predicted location
# # # ==============================
# # def plot_location(pred_point):
# #     plt.figure(figsize=(6,6))
# #     plt.grid(True, linestyle='--', alpha=0.4)
# #     plt.title("📍 Indoor Bluetooth Localization", fontsize=14)

# #     # Plot all calibration points
# #     for point, row in coords.iterrows():
# #         plt.scatter(row.x, row.y, c='gray', s=100)
# #         plt.text(row.x + 0.05, row.y + 0.05, point, fontsize=12)

# #     # Highlight predicted point
# #     px, py = coords.loc[pred_point]
# #     plt.scatter(px, py, c='red', s=200, label=f"Predicted: {pred_point}")
# #     plt.legend()
# #     plt.xlabel("X Position")
# #     plt.ylabel("Y Position")
# #     plt.show()

# # # ==============================
# # # 5️⃣ BLE Real-Time Scanning
# # # ==============================

# # # Target Beacon MAC Address
# # TARGET_MAC = "4D:F0:32:59:D0:BA"   # ← your actual beacon MAC


# # async def scan_live():
# #     print("\n🔍 Starting BLE scan... Press Ctrl+C to stop.\n")

# #     while True:
# #         devices = await BleakScanner.discover()

# #         # Search for the target beacon
# #         target = next((d for d in devices if d.address.upper() == TARGET_MAC.upper()), None)

# #         if target:
# #             rssi_val = target.rssi
# #             print(f"📡 RSSI Received = {rssi_val} dBm")

# #             # Predict location
# #             point, diff, (x, y) = predict_location(rssi_val)
# #             print(f"➡️ Predicted Location: {point} (x={x}, y={y}) — Difference = {diff:.1f} dBm\n")

# #             plot_location(point)
# #         else:
# #             print("⚠️ Beacon not found...")

# #         await asyncio.sleep(1)   # Delay between scans


# # # ==============================
# # # 6️⃣ Run the system
# # # ==============================
# # asyncio.run(scan_live())
# import pandas as pd
# import numpy as np
# from matplotlib import pyplot as plt
# from bleak import BleakScanner
# import asyncio

# # ==============================
# # 1️⃣ Load training data
# # ==============================
# file_name = "training.xlsx"
# df = pd.read_excel(file_name)

# print("✅ Training data loaded:")
# print(df.head(), "\n")

# # ==============================
# # 2️⃣ Compute fingerprint (average RSSI per point)
# # ==============================
# fingerprint = df.groupby('point_id')['rssi'].mean()
# coords = df.groupby('point_id')[['x', 'y']].first()

# # ==============================
# # 3️⃣ Predict location function
# # ==============================
# def predict_location(rssi_value):
#     diffs = abs(fingerprint - rssi_value)
#     best_match = diffs.idxmin()
#     best_score = diffs.min()
#     x, y = coords.loc[best_match]
#     return best_match, best_score, (x, y)

# # ==============================
# # 4️⃣ Plot location
# # ==============================
# def plot_location(pred_point):
#     plt.figure(figsize=(6, 6))
#     plt.grid(True, linestyle='--', alpha=0.4)
#     plt.title("📍 Indoor Bluetooth Localization", fontsize=14)

#     # plot calibration points
#     for point, row in coords.iterrows():
#         plt.scatter(row.x, row.y, c='gray', s=100)
#         plt.text(row.x + 0.05, row.y + 0.05, point, fontsize=12)

#     # plot prediction
#     px, py = coords.loc[pred_point]
#     plt.scatter(px, py, c='red', s=200, label=f"Predicted: {pred_point}")
#     plt.legend()
#     plt.xlabel("X Position")
#     plt.ylabel("Y Position")
#     plt.show()

# # ==============================
# # 5️⃣ Real-time BLE scanning (correct Bleak version)
# # ==============================

# TARGET_MAC = "4D:F0:32:59:D0:BA"   # ← change to your beacon MAC (NO colon at end)


# async def scan_live():
#     print("\n🔍 Starting BLE scan (callback mode)... Press Ctrl+C to stop.\n")

#     # callback function for BLE packets
#     def detection_callback(device, advertisement_data):
#         if device.address.upper() == TARGET_MAC.upper():
#             rssi_val = advertisement_data.rssi

#             print(f"📡 RSSI Received = {rssi_val} dBm")

#             # location prediction
#             point, diff, (x, y) = predict_location(rssi_val)
#             print(f"➡️ Predicted Location: {point} (x={x}, y={y}) — Difference = {diff:.1f} dBm\n")

#             plot_location(point)

#     # Start scanner with callback
#     scanner = BleakScanner(detection_callback)
#     await scanner.start()

#     try:
#         while True:
#             await asyncio.sleep(1)
#     except KeyboardInterrupt:
#         print("🛑 Scan stopped by user")
#     finally:
#         await scanner.stop()


# # ==============================
# # 6️⃣ Run system
# # ==============================
# asyncio.run(scan_live())
import pandas as pd
import numpy as np
from matplotlib import pyplot as plt
from bleak import BleakScanner
import asyncio

# ==============================
# 1 Load training data
# ==============================
file_name = "training.xlsx"
df = pd.read_excel(file_name)

print("Training data loaded:")
print(df.head(), "\n")

# ==============================
# 2 Compute fingerprint (average RSSI per point)
# ==============================
fingerprint = df.groupby('point_id')['rssi'].mean()
coords = df.groupby('point_id')[['x', 'y']].first()

# ==============================
# 3 Predict location function
# ==============================
def predict_location(rssi_value):
    diffs = abs(fingerprint - rssi_value)
    best_match = diffs.idxmin()
    best_score = diffs.min()
    x, y = coords.loc[best_match]
    return best_match, best_score, (x, y)

# ==============================
# 4 Plot location
# ==============================
def plot_location(pred_point):
    plt.figure(figsize=(6, 6))
    plt.grid(True, linestyle='--', alpha=0.4)
    plt.title("Indoor Bluetooth Localization", fontsize=14)

    # plot calibration points
    for point, row in coords.iterrows():
        plt.scatter(row.x, row.y, c='gray', s=100)
        plt.text(row.x + 0.05, row.y + 0.05, point, fontsize=12)

    # plot prediction
    px, py = coords.loc[pred_point]
    plt.scatter(px, py, c='red', s=200, label=f"Predicted: {pred_point}")
    plt.legend()
    plt.xlabel("X Position")
    plt.ylabel("Y Position")
    plt.show()

# ==============================
# 5 Real-time BLE scanning
# ==============================

TARGET_MAC = "4D:F0:32:59:D0:BA"   # ← change this to your beacon MAC

async def scan_live():
    print("\nStarting BLE scan... Press Ctrl+C to stop.\n")

    # callback for BLE packets
    def detection_callback(device, advertisement_data):
        if device.address.upper() == TARGET_MAC.upper():
            rssi_val = advertisement_data.rssi

            print(f"RSSI Received = {rssi_val} dBm")

            # location prediction
            point, diff, (x, y) = predict_location(rssi_val)
            print(f"Predicted Location: {point} (x={x}, y={y}) - Difference = {diff:.1f} dBm\n")

            plot_location(point)

    # Start scanner
    scanner = BleakScanner(detection_callback)
    await scanner.start()

    try:
        while True:
            await asyncio.sleep(1)
    except KeyboardInterrupt:
        print("Scan stopped by user")
    finally:
        await scanner.stop()

# ==============================
# 6 Run system
# ==============================
asyncio.run(scan_live())
