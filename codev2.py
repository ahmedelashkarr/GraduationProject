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
    time.sleep(2)  # انتظر لثانيتين للنتائج
    return iface.scan_results()

# بداية البرنامج
print("=== RSSI Collector with Averages ===")
point_name = input("ادخل اسم النقطة (Point Name): ")
x = input("ادخل X: ")
y = input("ادخل Y: ")

print(f"جمع {READINGS_PER_POINT} قراءة لكل شبكة في النقطة {point_name}...")

# هيكل لتخزين كل القراءات لكل شبكة
network_readings = defaultdict(list)

# جمع القراءات
for i in range(READINGS_PER_POINT):
    results = scan_wifi()
    for network in results:
        ssid = network.ssid
        bssid = network.bssid
        rssi = network.signal
        # خزّن RSSI لكل شبكة (SSID + BSSID) عشان المتوسط
        network_readings[(ssid, bssid)].append(rssi)
    time.sleep(0.5)  # نصف ثانية بين كل scan لتقليل التذبذب

# حساب المتوسط لكل شبكة
avg_readings = []
for (ssid, bssid), rssi_list in network_readings.items():
    avg_rssi = sum(rssi_list) / len(rssi_list)
    avg_readings.append([point_name, x, y, ssid, bssid, round(avg_rssi, 2)])

# حفظ المتوسطات في CSV
with open(CSV_FILE, mode='a', newline='',encoding='utf-8') as file:
    writer = csv.writer(file)
    # لو الملف جديد، اضف العناوين
    if file.tell() == 0:
        writer.writerow(["Point", "X", "Y", "SSID", "BSSID", "Avg_RSSI"])
    for row in avg_readings:
        writer.writerow(row)

print(f"تم حفظ المتوسطات في {CSV_FILE} بنجاح!")
