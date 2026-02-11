import csv
import json
from collections import defaultdict
from statistics import mean

IN_CSV = "raw_scans.csv"
OUT_JSON = "radio_map.json"

def main():
    # radio_map[point_id][bssid] = avg rssi
    data = defaultdict(lambda: defaultdict(list))
    coords = {}

    with open(IN_CSV, newline="", encoding="utf-8") as f:
        r = csv.DictReader(f)
        for row in r:
            pid = row["point_id"]
            x = float(row["x"]); y = float(row["y"])
            bssid = row["bssid"].upper()
            rssi = float(row["rssi_dbm"])
            coords[pid] = {"x": x, "y": y}
            data[pid][bssid].append(rssi)

    radio_map = {}
    for pid, bssids in data.items():
        radio_map[pid] = {bssid: round(mean(vals), 2) for bssid, vals in bssids.items()}

    out = {"position_database": coords, "radio_map": radio_map}
    with open(OUT_JSON, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=2)

    print(f"Saved: {OUT_JSON}")
    print(f"Points: {len(radio_map)}")

if __name__ == "__main__":
    main()
