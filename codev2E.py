import pywifi
from pywifi import const
import time
import csv
from collections import defaultdict

# تهيئة الواي فاي
wifi = pywifi.PyWiFi()
iface = wifi.interfaces()[0]

# عدد القراءات لكل نقطة
READINGS_PER_POINT = 25

# اسم ملف CSV لتخزين المتوسطات
CSV_FILE = "wifi_avg_readings.csv"

# دالة لعمل scan
def scan_wifi():
    iface.scan()
    time.sleep(2)  
    return iface.scan_results()

print("=== RSSI Collector with Averages ===")
point_name = input("Enter Point Name: ")
x = input("Enter X coordinate: ")
y = input("Enter Y coordinate: ")

print(f"Collecting {READINGS_PER_POINT} readings per network at point {point_name}...")

network_readings = defaultdict(list)
target_ssids = ["WEF2B2C9"]

for i in range(READINGS_PER_POINT):
    results = scan_wifi()
    for network in results:
        ssid = network.ssid
        bssid = network.bssid
        rssi = network.signal
        network_readings[(ssid, bssid)].append(rssi)
    time.sleep(0.5)  

avg_readings = []
for (ssid, bssid), rssi_list in network_readings.items():
    avg_rssi = sum(rssi_list) / len(rssi_list)
    avg_readings.append([point_name, x, y, ssid, bssid, round(avg_rssi, 2)])

with open(CSV_FILE, mode='a', newline='', encoding='utf-8') as file:
    writer = csv.writer(file)
    if file.tell() == 0:
        writer.writerow(["Point", "X", "Y", "SSID", "BSSID", "Avg_RSSI"])
    for row in avg_readings:
        writer.writerow(row)

print(f"Averages saved successfully in {CSV_FILE}!")
