import pywifi
from pywifi import const
import time

wifi = pywifi.PyWiFi()
iface = wifi.interfaces()[0]

def scan_wifi():
    iface.scan()
    time.sleep(2)
    results = iface.scan_results()

    for network in results:
        print(f"SSID: {network.ssid}, RSSI: {network.signal}")

while True:
    print("Scanning...")
    scan_wifi()
    print("-------------------")
    time.sleep(2)
