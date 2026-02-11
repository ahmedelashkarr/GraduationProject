import pywifi
from pywifi import const
import time
import math
import csv

# اسم الشبكة المطلوب قياسها
TARGET_SSID = "Your_Wifi_Name"

# المسافات المعروفة للمعايرة (بالمتر)
CALIBRATION_DISTANCES = [1, 2, 3, 4]

# عدد القراءات لكل مسافة
READINGS_PER_DISTANCE = 20

wifi = pywifi.PyWiFi()
iface = wifi.interfaces()[0]


def scan_rssi():
    iface.scan()
    time.sleep(2)
    results = iface.scan_results()
    for network in results:
        if network.ssid == TARGET_SSID:
            return network.signal
    return None


def get_average_rssi():
    readings = []
    for _ in range(READINGS_PER_DISTANCE):
        rssi = scan_rssi()
        if rssi:
            readings.append(rssi)
        time.sleep(0.5)

    if readings:
        return sum(readings) / len(readings)
    return None


def calculate_environment_factor(rssi1m, rssi_d, distance):
    """
    حساب معامل البيئة n
    """
    return (rssi1m - rssi_d) / (10 * math.log10(distance))


print("=== Automatic Calibration Started ===")
input("قف على مسافة 1 متر من الراوتر ثم اضغط Enter...")

rssi_1m = get_average_rssi()
print(f"RSSI at 1m: {rssi_1m}")

n_values = []

for d in CALIBRATION_DISTANCES[1:]:
    input(f"قف على مسافة {d} متر ثم اضغط Enter...")
    rssi_d = get_average_rssi()

    if rssi_d:
        n = calculate_environment_factor(rssi_1m, rssi_d, d)
        n_values.append(n)
        print(f"Distance {d}m → RSSI: {rssi_d:.2f}, n: {n:.2f}")

if n_values:
    n_avg = sum(n_values) / len(n_values)
    print(f"\nFinal Environment Factor (n): {n_avg:.2f}")

    # حفظ النتيجة في ملف
    with open("calibration_result.csv", "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["RSSI_1m", "Environment_n"])
        writer.writerow([rssi_1m, n_avg])

    print("Calibration saved to calibration_result.csv")
else:
    print("Calibration failed. No valid readings.")
