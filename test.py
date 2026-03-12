# import csv
# import pywifi
# import time
# from collections import defaultdict, Counter, deque
# import math

# CSV_FILE = "wifi_avg_readings.csv"
# target_ssids = ["ZOoz", "TP-Link_7B34", "Dahab"]

# wifi = pywifi.PyWiFi()
# iface = wifi.interfaces()[0]

# # حجم الذاكرة للتصويت
# HISTORY_SIZE = 10
# point_history = deque(maxlen=HISTORY_SIZE)


# # جمع متوسط القراءة الحالية
# def get_average_reading(samples=15):
#     readings = defaultdict(list)

#     for _ in range(samples):
#         iface.scan()
#         time.sleep(1.5)
#         results = iface.scan_results()

#         for net in results:
#             if net.ssid in target_ssids:
#                 readings[net.ssid].append(net.signal)

#     avg = {}
#     for ssid in target_ssids:
#         if ssid in readings:
#             avg[ssid] = sum(readings[ssid]) / len(readings[ssid])
#         else:
#             avg[ssid] = -100  # شبكة غير موجودة

#     return avg


# # تحميل قاعدة البيانات
# def load_db():
#     db = defaultdict(dict)
#     with open(CSV_FILE, newline='', encoding='utf-8') as f:
#         reader = csv.DictReader(f)
#         for row in reader:
#             try:
#                 point = row["Point"]
#                 ssid = row["SSID"]
#                 rssi = float(row["Avg_RSSI"])
#                 x = float(row["X"])
#                 y = float(row["Y"])

#                 db[point]["coords"] = (x, y)
#                 db[point][ssid] = rssi
#             except:
#                 continue
#     return db


# # حساب المسافة
# def distance(fp1, fp2):
#     d = 0
#     for ssid in target_ssids:
#         rssi1 = fp1.get(ssid, -100)
#         rssi2 = fp2.get(ssid, -100)
#         d += (rssi1 - rssi2) ** 2
#     return math.sqrt(d)


# # تحديد أقرب نقطة
# def locate_best_point(current, db):
#     best_point = None
#     best_distance = float("inf")
#     best_coords = None

#     for point, data in db.items():
#         dist = distance(current, data)
#         if dist < best_distance:
#             best_distance = dist
#             best_point = point
#             best_coords = data["coords"]

#     return best_point, best_coords, best_distance


# # البرنامج الرئيسي
# db = load_db()

# print("Stable WiFi positioning started...")
# print("Press Ctrl+C to stop.\n")

# try:
#     while True:
#         current = get_average_reading()
#         point, coords, dist = locate_best_point(current, db)

#         # إضافة النقطة للتاريخ
#         point_history.append(point)

#         # حساب التصويت
#         most_common = Counter(point_history).most_common(1)[0][0]

#         stable_coords = db[most_common]["coords"]

#         print(
#             f"Instant: {point} | score: {round(dist,2)}  "
#             f"→ Stable: {most_common} | coords: {stable_coords}"
#         )

#         time.sleep(1)

# except KeyboardInterrupt:
#     print("\nStopped.")
import csv
import pywifi
import time
from collections import defaultdict, Counter, deque
import math

CSV_FILE = "wifi_avg_readings.csv"
target_ssids = ["ZOoz", "TP-Link_7B34", "Dahab"]

wifi = pywifi.PyWiFi()
iface = wifi.interfaces()[0]

# عدد العينات لكل قراءة
SAMPLES = 15

# حجم الذاكرة للتصويت
HISTORY_SIZE = 6
point_history = deque(maxlen=HISTORY_SIZE)

# عدد أقرب نقاط لحساب KNN
K = 3


def get_average_reading(samples=SAMPLES):
    readings = defaultdict(list)
    for _ in range(samples):
        iface.scan()
        time.sleep(1.2)
        results = iface.scan_results()
        for net in results:
            if net.ssid in target_ssids:
                readings[net.ssid].append(net.signal)
    avg = {}
    for ssid in target_ssids:
        if ssid in readings and len(readings[ssid]) > 0:
            avg[ssid] = sum(readings[ssid]) / len(readings[ssid])
        else:
            avg[ssid] = -85  # تصحيح الشبكات غير الموجودة
    return avg


def load_db():
    db = defaultdict(dict)
    with open(CSV_FILE, newline='', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        for row in reader:
            try:
                point = row["Point"]
                ssid = row["SSID"]
                rssi = float(row["Avg_RSSI"])
                x = float(row["X"])
                y = float(row["Y"])
                db[point]["coords"] = (x, y)
                db[point][ssid] = rssi
            except:
                continue
    return db


def distance(fp1, fp2):
    # المسافة بين قراءة WiFi وقاعدة البيانات
    d = 0
    for ssid in target_ssids:
        d += (fp1.get(ssid, -85) - fp2.get(ssid, -85)) ** 2
    return math.sqrt(d)


def locate_knn(current, db, k=K):
    # احسب المسافة لكل النقاط
    distances = []
    for point, data in db.items():
        dist = distance(current, data)
        distances.append((dist, point, data["coords"]))
    # ترتيب النقاط حسب أقرب مسافة
    distances.sort(key=lambda x: x[0])
    nearest = distances[:k]
    # المتوسط لإحداثيات KNN
    avg_x = sum(p[2][0] for p in nearest) / k
    avg_y = sum(p[2][1] for p in nearest) / k
    # استخدم التصويت لأكثر نقطة متكررة
    most_common_point = Counter([p[1] for p in nearest]).most_common(1)[0][0]
    return most_common_point, (avg_x, avg_y), nearest


# تحميل قاعدة البيانات
db = load_db()

print("Smart WiFi positioning started...")
print("Press Ctrl+C to stop.\n")

last_stable = None

try:
    while True:
        current = get_average_reading()
        point, avg_coords, nearest = locate_knn(current, db)

        # تحديث التاريخ للتصويت
        point_history.append(point)
        stable_point = Counter(point_history).most_common(1)[0][0]
        stable_coords = db[stable_point]["coords"]

        # عرض النتائج فقط إذا تغيرت النقطة المستقرة
        if stable_point != last_stable:
            print(f"You are now at: {stable_point} | coords: {stable_coords}")
            last_stable = stable_point

        time.sleep(1)

except KeyboardInterrupt:
    print("\nStopped.")
