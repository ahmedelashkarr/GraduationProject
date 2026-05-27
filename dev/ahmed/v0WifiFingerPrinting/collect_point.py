import csv
import time
import json
import subprocess
from statistics import mean
from collections import defaultdict

IFACE = None          # مثال: "wlp3s0" أو سيبه None
SCANS = 25
SLEEP_BETWEEN = 0.8
OUT_CSV = "raw_scans.csv"
TARGETS_JSON = "targets_3bssid.json"


def parse_nmcli_lines(out: str):
    rows = []
    for line in out.strip().splitlines():
        parts = line.split(":")
        if len(parts) < 8:
            continue
        bssid = ":".join(parts[0:6]).upper()
        signal_str = parts[6]
        ssid = ":".join(parts[7:])
        if not signal_str.isdigit():
            continue
        signal = int(signal_str)
        rssi_dbm = int((signal / 2) - 100)
        rows.append((bssid, ssid, signal, rssi_dbm))
    return rows


def run_nmcli_scan():
    cmd = ["nmcli", "-t", "-f", "BSSID,SIGNAL,SSID", "dev", "wifi", "list", "--rescan", "yes"]
    if IFACE:
        cmd += ["ifname", IFACE]
    out = subprocess.check_output(cmd, text=True, errors="ignore")
    return parse_nmcli_lines(out)


def choose_targets_interactive():
    scan = run_nmcli_scan()
    if not scan:
        raise RuntimeError("No Wi-Fi networks found by nmcli.")

    # رتب بالأقوى (signal أعلى / rssi أقرب للصفر)
    scan_sorted = sorted(scan, key=lambda x: x[2], reverse=True)

    print("\nTop visible networks (strongest first):")
    for idx, (bssid, ssid, signal, rssi) in enumerate(scan_sorted[:15], 1):
        print(f"{idx:2d}) {bssid} | SSID='{ssid}' | SIGNAL={signal}% | RSSI≈{rssi} dBm")

    print("\nChoose 3 networks by number (e.g. 1 3 5):")
    picks = input("Your picks: ").strip().split()
    if len(picks) != 3:
        raise ValueError("Please pick exactly 3 numbers.")

    chosen = []
    for p in picks:
        i = int(p) - 1
        bssid, ssid, signal, rssi = scan_sorted[i]
        chosen.append({"bssid": bssid, "ssid": ssid})

    with open(TARGETS_JSON, "w", encoding="utf-8") as f:
        json.dump(chosen, f, ensure_ascii=False, indent=2)

    print(f"\nSaved targets to {TARGETS_JSON}:")
    for t in chosen:
        print(f"  - {t['bssid']} (SSID='{t['ssid']}')")

    return [t["bssid"] for t in chosen]


def load_targets():
    with open(TARGETS_JSON, encoding="utf-8") as f:
        chosen = json.load(f)
    return [t["bssid"].upper() for t in chosen]


def collect_point(point_id, x, y, targets_bssid, scans=SCANS):
    all_readings = []  # (point_id, x, y, scan_idx, bssid, ssid, rssi_dbm)

    targets_set = set(t.upper() for t in targets_bssid)

    for i in range(scans):
        scan = run_nmcli_scan()

        # فلترة: خليك بس في الـ 3 BSSID اللي اخترتهم
        scan = [row for row in scan if row[0].upper() in targets_set]

        # لو في scan معيّن بعض الـ BSSID مختفوش، دي حاجة طبيعية
        for (bssid, ssid, signal, rssi_dbm) in scan:
            all_readings.append((point_id, x, y, i, bssid, ssid, rssi_dbm))

        time.sleep(SLEEP_BETWEEN)

    return all_readings


def main():
    # targets
    try:
        targets = load_targets()
        print(f"Loaded targets from {TARGETS_JSON}")
    except Exception:
        print(f"Targets file not found. Let's choose 3 networks now...")
        targets = choose_targets_interactive()

    point_id = input("\nPoint ID (e.g. living_center): ").strip()
    x = float(input("x (meters): ").strip())
    y = float(input("y (meters): ").strip())

    readings = collect_point(point_id, x, y, targets)

    # حفظ raw
    with open(OUT_CSV, "a", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        if f.tell() == 0:
            w.writerow(["point_id","x","y","scan_idx","bssid","ssid","rssi_dbm"])
        w.writerows(readings)

    # fingerprint متوسط لكل BSSID من التلاتة
    by_bssid = defaultdict(list)
    for r in readings:
        by_bssid[r[4]].append(r[6])

    fingerprint = {bssid: round(mean(vals), 2) for bssid, vals in by_bssid.items()}

    print("\nFingerprint (avg RSSI per chosen BSSID):")
    for bssid in targets:
        if bssid in fingerprint:
            print(f"  {bssid}: {fingerprint[bssid]} dBm")
        else:
            print(f"  {bssid}: NOT SEEN (try increasing scans / move slightly)")

    print(f"\nSaved raw scans to: {OUT_CSV}")


if __name__ == "__main__":
    main()
