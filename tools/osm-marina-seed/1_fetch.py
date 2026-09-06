#!/usr/bin/env python3
"""Fetch Turkey marinas + fishing harbours + province polygons from Overpass.

Data (c) OpenStreetMap contributors, ODbL. Writes raw JSON into ./raw/.
Run:  python3 1_fetch.py
"""
import json, sys, time, urllib.request, urllib.parse, os

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw")
os.makedirs(RAW, exist_ok=True)

ENDPOINTS = [
    "https://overpass-api.de/api/interpreter",
    "https://overpass.kumi.systems/api/interpreter",
    "https://maps.mail.ru/osm/tools/overpass/api/interpreter",
]

Q_MARINA = """
[out:json][timeout:240];
area["ISO3166-1"="TR"][admin_level=2]->.tr;
( nwr["leisure"="marina"](area.tr); );
out center tags;
"""

Q_FISHING = """
[out:json][timeout:240];
area["ISO3166-1"="TR"][admin_level=2]->.tr;
(
  nwr["harbour"="fishing"](area.tr);
  nwr["harbour:category"="fishing"](area.tr);
  nwr["seamark:harbour:category"~"fishing"](area.tr);
  nwr["name"~"Balıkçı Barınağı",i](area.tr);
);
out center tags;
"""

# Province polygons for local point-in-polygon. `out geom` returns member way
# geometry we stitch into rings in the province step.
Q_PROVINCES = """
[out:json][timeout:300];
area["ISO3166-1"="TR"][admin_level=2]->.tr;
rel["admin_level"="4"]["boundary"="administrative"](area.tr);
out geom;
"""


def run(query, label):
    body = urllib.parse.urlencode({"data": query}).encode()
    last = None
    for ep in ENDPOINTS:
        for attempt in range(3):
            try:
                print(f"[{label}] {ep} (try {attempt+1})", file=sys.stderr)
                req = urllib.request.Request(ep, data=body, headers={"User-Agent": "inktavia-marina-seed/1.0"})
                with urllib.request.urlopen(req, timeout=300) as r:
                    data = json.loads(r.read().decode())
                n = len(data.get("elements", []))
                print(f"[{label}] OK — {n} elements", file=sys.stderr)
                return data
            except Exception as e:
                last = e
                print(f"[{label}] failed: {e}", file=sys.stderr)
                time.sleep(8 * (attempt + 1))
        time.sleep(4)
    raise SystemExit(f"[{label}] all endpoints failed: {last}")


def main():
    for label, q in (("marinas", Q_MARINA), ("fishing", Q_FISHING), ("provinces", Q_PROVINCES)):
        data = run(q, label)
        path = os.path.join(RAW, f"{label}.json")
        with open(path, "w") as f:
            json.dump(data, f)
        print(f"wrote {path} ({len(data.get('elements', []))} elements)")
        time.sleep(6)


if __name__ == "__main__":
    main()
