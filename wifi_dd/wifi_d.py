import pywifi
import time
import math

# اسم الشبكة
TARGET_SSID = "WEF2B2C9"   # غيرها حسب الشبكة اللي عايز تقيس منها

# قيم المعايرة (غيرهم بعد الاختبار)
A = -27.7    # RSSI على مسافة 1 متر
n = 2.2    # معامل البيئة (غرفة عادية)

wifi = pywifi.PyWiFi()
iface = wifi.interfaces()[0]


# دالة لجمع متوسط RSSI
def get_average_rssi(samples=10):
    readings = []

    for _ in range(samples):
        iface.scan()
        time.sleep(1.5)
        results = iface.scan_results()

        for net in results:
            if net.ssid == TARGET_SSID:
                readings.append(net.signal)

    if len(readings) == 0:
        return None

    return sum(readings) / len(readings)


# تحويل RSSI إلى مسافة
def rssi_to_distance(rssi):
    return 10 ** ((A - rssi) / (10 * n))


print("WiFi distance test started...")
print("Press Ctrl+C to stop\n")

try:
    while True:
        rssi = get_average_rssi()

        if rssi is None:
            print("Network not found")
        else:
            distance = rssi_to_distance(rssi)
            print(
                f"RSSI: {round(rssi,2)} dBm | "
                f"Estimated distance: {round(distance,2)} meters"
            )

        time.sleep(1)

except KeyboardInterrupt:
    print("\nStopped.")
