import pywifi
from pywifi import const
import time
import csv
from collections import defaultdict

# تهيئة الواي فاي
wifi = pywifi.PyWiFi()
iface = wifi.interfaces()[0]

# عدد القراءات لكل نقطة
READINGS_PER_POINT = 50

# اسم ملف CSV لتخزين المتوسطات
CSV_FILE = "wifi_avg_readings.csv"

# دالة لعمل scan
def scan_wifi():
    iface.scan()
    time.sleep(2)  # انتظر لثانيتين للنتائج
    return iface.scan_results()

# بداية البرنامج
print("=== RSSI Collector with Averages ===")

# ادخل النقطة والإحداثيات
point_name = input("Enter Point Name: ")
x = input("Enter X coordinate: ")
y = input("Enter Y coordinate: ")

# ادخل الشبكات اللي عايز تجمع لها RSSI
ssid_input = input("Enter target SSIDs (comma separated): ")
target_ssids = [s.strip() for s in ssid_input.split(",")]

print(f"Collecting {READINGS_PER_POINT} readings per target network at point {point_name}...")
print(f"Target networks: {target_ssids}")

# هيكل لتخزين كل القراءات لكل شبكة
network_readings = defaultdict(list)

# جمع القراءات
for i in range(READINGS_PER_POINT):
    results = scan_wifi()
    for network in results:
        ssid = network.ssid
        bssid = network.bssid
        rssi = network.signal
        # اجمع فقط الشبكات المطلوبة
        if ssid in target_ssids:
            network_readings[(ssid, bssid)].append(rssi)
    time.sleep(0.5)  # نصف ثانية بين كل scan لتقليل التذبذب

# حساب المتوسط لكل شبكة
avg_readings = []
for (ssid, bssid), rssi_list in network_readings.items():
    avg_rssi = sum(rssi_list) / len(rssi_list)
    avg_readings.append([point_name, x, y, ssid, bssid, round(avg_rssi, 2)])

# حفظ المتوسطات في CSV
with open(CSV_FILE, mode='a', newline='', encoding='utf-8') as file:
    writer = csv.writer(file)
    # لو الملف جديد، اضف العناوين
    if file.tell() == 0:
        writer.writerow(["Point", "X", "Y", "SSID", "BSSID", "Avg_RSSI"])
    for row in avg_readings:
        writer.writerow(row)

print(f"Averages saved successfully in {CSV_FILE}!")
