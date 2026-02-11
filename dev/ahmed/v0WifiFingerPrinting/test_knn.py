import json
import math
import time
import subprocess
from collections import Counter, defaultdict

# ====== SETTINGS ======
NUM_READINGS = 5       # عدد القراءات اللي هتتجمع قبل ما يحدد المكان
SLEEP_BETWEEN = 0.3      # وقت بسيط بين كل قراءة
RESCAN_EVERY = 3         # اعمل rescan yes كل كام قراءة (لتسريع الموضوع)
K = 3
MIN_COMMON_APS = 3
# ======================


def scan_now(rescan: bool = True):
    cmd = ["nmcli", "-t", "-f", "BSSID,SIGNAL", "dev", "wifi", "list",
           "--rescan", "yes" if rescan else "no"]
    out = subprocess.check_output(cmd, text=True, errors="ignore")
    rssi = {}
    for line in out.strip().splitlines():
        parts = line.split(":")
        if len(parts) < 7:
            continue
        bssid = ":".join(parts[0:6]).upper()
        sig = parts[6]
        if not sig.isdigit():
            continue
        sig = int(sig)
        rssi_dbm = int((sig / 2) - 100)  # تقريب
        rssi[bssid] = rssi_dbm
    return rssi


def collect_n_scans(n=NUM_READINGS, sleep_between=SLEEP_BETWEEN):
    scans = []
    for i in range(n):
        rescan = (i % RESCAN_EVERY == 0)  # أسرع: مش كل مرة rescan
        scans.append(scan_now(rescan=rescan))
        time.sleep(sleep_between)
    return scans


def average_scans(scans):
    """
    Average RSSI per BSSID across multiple scans.
    لو BSSID مش ظاهر في كل scans، بناخد متوسط اللي ظهر فيه بس.
    """
    values = defaultdict(list)
    for s in scans:
        for bssid, rssi in s.items():
            values[bssid].append(rssi)

    avg = {}
    for bssid, arr in values.items():
        avg[bssid] = sum(arr) / len(arr)
    return avg


def euclid(curr, ref):
    common = set(curr) & set(ref)
    if not common:
        return float("inf")

    s = 0.0
    for b in common:
        d = curr[b] - ref[b]
        s += d * d

    penalty = 0.0
    if len(common) < MIN_COMMON_APS:
        penalty = 50.0  # عقوبة لو المشترك قليل
    return math.sqrt(s) + penalty


def knn(curr, radio_map, k=K):
    dists = []
    for pid, fp in radio_map.items():
        dists.append((pid, euclid(curr, fp)))
    dists.sort(key=lambda x: x[1])
    return dists[:k]


def majority_vote(predictions):
    """
    predictions: list of position_id
    """
    c = Counter(predictions)
    best, count = c.most_common(1)[0]
    confidence = count / len(predictions)
    return best, confidence, c


def main():
    with open("radio_map.json", encoding="utf-8") as f:
        db = json.load(f)
    radio_map = db["radio_map"]
    pos_db = db["position_database"]

    print(f"Collecting {NUM_READINGS} Wi-Fi scans...")
    scans = collect_n_scans(NUM_READINGS, SLEEP_BETWEEN)

    # 1) Average method (أفضل عادة)
    curr_avg = average_scans(scans)
    neighbors_avg = knn(curr_avg, radio_map, k=K)

    print("\n--- Result using AVERAGED RSSI ---")
    print("K nearest:")
    for pid, d in neighbors_avg:
        print(f"  {pid}: dist={d:.2f}")

    best_avg = neighbors_avg[0][0]
    est_avg = pos_db[best_avg]
    print(f"Estimated position (avg): {best_avg} -> ({est_avg['x']}, {est_avg['y']})")

    # 2) Vote method (اختياري)
    per_scan_preds = []
    for s in scans:
        nbs = knn(s, radio_map, k=K)
        per_scan_preds.append(nbs[0][0])

    best_vote, vote_conf, vote_counts = majority_vote(per_scan_preds)
    est_vote = pos_db[best_vote]

    print("\n--- Result using MAJORITY VOTE over scans ---")
    print(f"Estimated position (vote): {best_vote} -> ({est_vote['x']}, {est_vote['y']})")
    print(f"Vote confidence: {vote_conf:.2f}")
    print("Vote counts:", dict(vote_counts))

    # لو انت واقف في نقطة معلومة وعايز تحسب error:
    true_id = input("\nIf you are at a known point, enter its point_id (or press Enter): ").strip()
    if true_id and true_id in pos_db:
        tx, ty = pos_db[true_id]["x"], pos_db[true_id]["y"]

        ex, ey = est_avg["x"], est_avg["y"]
        err_avg = math.sqrt((ex - tx) ** 2 + (ey - ty) ** 2)

        vx, vy = est_vote["x"], est_vote["y"]
        err_vote = math.sqrt((vx - tx) ** 2 + (vy - ty) ** 2)

        print(f"\nError (avg)  vs {true_id}: {err_avg:.2f} m")
        print(f"Error (vote) vs {true_id}: {err_vote:.2f} m")


if __name__ == "__main__":
    main()
