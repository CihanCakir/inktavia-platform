#!/usr/bin/env python3
"""Build the reviewed Turkey marina + fishing-harbour seed from raw Overpass data.

Data (c) OpenStreetMap contributors, ODbL.

Pipeline: extract points -> province via local point-in-polygon (admin_level=4)
-> map province->cityCode (plate) via the Location seed -> clean/dedupe/name
-> reconcile the interim 22 hand marinas (OSM wins, keep existing code) ->
emit marinas.seed.json + marinas.review.csv.

Run:  python3 2_build.py
"""
import json, os, re, math, csv, unicodedata

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw")
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
CITIES = os.path.join(REPO, "Modules/ReferenceData/src/Aizen.Modules.ReferenceData.Repository/Seed/Json/Location/TR/cities.json")
# Frozen snapshot of the original 22 hand-seeded marinas (kept separate from the SEED output path so
# re-runs never read their own output as "interim").
INTERIM = os.path.join(HERE, "interim_marinas.json")
SEED_DIR = os.path.join(REPO, "Modules/ReferenceData/src/Aizen.Modules.ReferenceData.Repository/Seed/Json/Marina")
SEED_JSON = os.path.join(SEED_DIR, "marinas.json")          # <- the file the seeder reads (REPLACES interim)
SEED_ATTR = os.path.join(SEED_DIR, "ATTRIBUTION.txt")
OUT_CSV = os.path.join(HERE, "out", "marinas.review.csv")
os.makedirs(os.path.join(HERE, "out"), exist_ok=True)

ATTRIBUTION = "Data (c) OpenStreetMap contributors, ODbL (https://www.openstreetmap.org/copyright)"

# ---------- helpers ----------

def strip_tr(s):
    """Turkish-aware ASCII fold for slugs/comparison."""
    if not s:
        return ""
    repl = {"ı": "i", "İ": "i", "ş": "s", "Ş": "s", "ğ": "g", "Ğ": "g",
            "ü": "u", "Ü": "u", "ö": "o", "Ö": "o", "ç": "c", "Ç": "c"}
    s = "".join(repl.get(ch, ch) for ch in s)
    s = unicodedata.normalize("NFKD", s).encode("ascii", "ignore").decode()
    return s

def slugify(s, maxlen=40):
    s = strip_tr(s).upper()
    s = re.sub(r"[^A-Z0-9]+", "_", s).strip("_")
    return s[:maxlen].strip("_")

def norm_name(s):
    """Comparison core: fold, upper, drop generic marina/harbour words + punctuation."""
    s = strip_tr(s or "").upper()
    s = re.sub(r"[^A-Z0-9 ]+", " ", s)
    for w in ("MARINASI", "MARINA", "YAT LIMANI", "YAT LIMAN", "BALIKCI BARINAGI",
              "BARINAGI", "BALIKCI", "LIMANI", "PORT", "HARBOUR", "HARBOR"):
        s = s.replace(w, " ")
    return re.sub(r"\s+", " ", s).strip()

def haversine_m(a_lat, a_lng, b_lat, b_lng):
    R = 6_371_000.0
    p1, p2 = math.radians(a_lat), math.radians(b_lat)
    dphi = math.radians(b_lat - a_lat)
    dl = math.radians(b_lng - a_lng)
    h = math.sin(dphi/2)**2 + math.cos(p1)*math.cos(p2)*math.sin(dl/2)**2
    return R * 2 * math.asin(math.sqrt(h))

def coord_of(el):
    if el["type"] == "node":
        return el.get("lat"), el.get("lon")
    c = el.get("center")
    return (c["lat"], c["lon"]) if c else (None, None)

# ---------- province polygons (stitch rings, PIP) ----------

def build_provinces():
    data = json.load(open(os.path.join(RAW, "provinces.json")))
    provinces = []
    for el in data["elements"]:
        if el.get("type") != "relation":
            continue
        name = el.get("tags", {}).get("name")
        if not name:
            continue
        segs = []
        for m in el.get("members", []):
            if m.get("role") in ("outer", "") and m.get("geometry"):
                segs.append([(g["lat"], g["lon"]) for g in m["geometry"]])
        rings = stitch(segs)
        if rings:
            provinces.append({"name": name, "rings": rings,
                              "bbox": bbox_of(rings)})
    return provinces

def stitch(segs):
    def key(p):
        return (round(p[0], 7), round(p[1], 7))
    segs = [s for s in segs if len(s) >= 2]
    rings = []
    used = [False]*len(segs)
    for i in range(len(segs)):
        if used[i]:
            continue
        used[i] = True
        ring = list(segs[i])
        changed = True
        while key(ring[0]) != key(ring[-1]) and changed:
            changed = False
            for j in range(len(segs)):
                if used[j]:
                    continue
                s = segs[j]
                if key(s[0]) == key(ring[-1]):
                    ring.extend(s[1:]); used[j] = True; changed = True
                elif key(s[-1]) == key(ring[-1]):
                    ring.extend(reversed(s[:-1])); used[j] = True; changed = True
                elif key(s[-1]) == key(ring[0]):
                    ring = s[:-1] + ring; used[j] = True; changed = True
                elif key(s[0]) == key(ring[0]):
                    ring = list(reversed(s[1:])) + ring; used[j] = True; changed = True
        if len(ring) >= 4:
            rings.append(ring)
    return rings

def bbox_of(rings):
    lats = [p[0] for r in rings for p in r]
    lngs = [p[1] for r in rings for p in r]
    return (min(lats), min(lngs), max(lats), max(lngs))

def point_in_ring(lat, lng, ring):
    inside = False
    n = len(ring)
    j = n - 1
    for i in range(n):
        yi, xi = ring[i]
        yj, xj = ring[j]
        if ((yi > lat) != (yj > lat)) and (lng < (xj - xi) * (lat - yi) / (yj - yi + 1e-18) + xi):
            inside = not inside
        j = i
    return inside

def province_of(lat, lng, provinces):
    for pr in provinces:
        mnlat, mnlng, mxlat, mxlng = pr["bbox"]
        if not (mnlat-0.02 <= lat <= mxlat+0.02 and mnlng-0.02 <= lng <= mxlng+0.02):
            continue
        if any(point_in_ring(lat, lng, r) for r in pr["rings"]):
            return pr["name"], False
    # Fallback: nearest province by min vertex distance (marinas sit on the coast, often
    # a few metres seaward of the admin polygon). Only flag for review when genuinely far
    # (>2 km) — a coastal marina just offshore of its own province is not a data problem.
    best, bestd = None, 1e18
    for pr in provinces:
        for r in pr["rings"]:
            for (y, x) in r[::5]:  # sample every 5th vertex for speed
                d = (y-lat)**2 + (x-lng)**2
                if d < bestd:
                    bestd, best = d, pr["name"]
    approx_m = math.sqrt(bestd) * 111_000  # deg -> m (coarse)
    return best, (approx_m > 2000)

# ---------- city (plate) map ----------

def city_map():
    cities = json.load(open(CITIES))
    m = {}
    for c in cities:
        m[norm_name(c["name"]["tr"])] = c["cityCode"]
        m[strip_tr(c["name"]["tr"]).upper()] = c["cityCode"]
    return m

# ---------- main ----------

def load_elements(fname, kind_default):
    data = json.load(open(os.path.join(RAW, fname)))
    out = []
    for el in data["elements"]:
        lat, lng = coord_of(el)
        if lat is None:
            continue
        tags = el.get("tags", {})
        out.append({
            "osmId": f"{el['type'][0]}{el['id']}",
            "osmType": el["type"],
            "lat": round(float(lat), 7),
            "lng": round(float(lng), 7),
            "name": (tags.get("name") or tags.get("name:tr") or "").strip(),
            "tags": tags,
            "kind": kind_default,
        })
    return out

def classify(el):
    """Refine type. Name wins: anything named 'Balıkçı Barınağı' is a fishing shelter even if
    OSM also tagged leisure=marina. Otherwise leisure=marina -> MARINA, else FISHING_HARBOR."""
    t = el["tags"]
    name = strip_tr(el.get("name") or "").lower()
    if "balikci barinagi" in name or t.get("harbour") == "fishing" or t.get("harbour:category") == "fishing":
        if t.get("leisure") == "marina" and "balikci" not in name:
            return "MARINA"
        return "FISHING_HARBOR"
    if el["kind"] == "MARINA" or t.get("leisure") == "marina":
        return "MARINA"
    return "FISHING_HARBOR"

STRAY_RE = re.compile(r"oil terminal|tersane|shipyard|naval|deniz\s*üss|askeri|container|konteyner|"
                      r"cruise terminal|lng|refinery|rafineri|dockyard|tanker", re.I)

def is_stray(el):
    """Obvious non-recreational facilities mistagged leisure=marina (oil/LNG terminals, shipyards, naval)."""
    return bool(STRAY_RE.search(el.get("name") or ""))

def district_hint(el):
    t = el["tags"]
    for k in ("addr:district", "is_in:town", "addr:city", "is_in:city", "addr:suburb"):
        if t.get(k):
            return t[k]
    return None

def main():
    print("building provinces (stitching rings)...")
    provinces = build_provinces()
    print(f"  {len(provinces)} province polygons")
    cmap = city_map()

    marinas = load_elements("marinas.json", "MARINA")
    fishing = load_elements("fishing.json", "FISHING_HARBOR")
    rows = marinas + fishing
    for el in rows:
        el["type"] = classify(el)
    strays = [e for e in rows if is_stray(e)]
    rows = [e for e in rows if not is_stray(e)]
    print(f"raw: {len(marinas)} marina elements, {len(fishing)} fishing elements; "
          f"dropped {len(strays)} strays ({', '.join(sorted(s['name'] for s in strays)[:6])})")

    # dedupe: <300m + (similar name OR marina/fishing overlap). MARINA wins over FISHING;
    # a real name wins over a blank; otherwise the element with more tags wins.
    rows.sort(key=lambda e: (0 if e["type"] == "MARINA" else 1, 0 if e["name"] else 1, -len(e["tags"])))
    kept = []
    dropped = 0
    for el in rows:
        dup = False
        for k in kept:
            d = haversine_m(el["lat"], el["lng"], k["lat"], k["lng"])
            if d > 300:
                continue
            en, kn = norm_name(el["name"]), norm_name(k["name"])
            same_name = en and kn and (en == kn or en in kn or kn in en)
            # Merge only genuine duplicates of the same facility: same/similar name within
            # 300 m, OR essentially the same point (<80 m) regardless of name/type. Two
            # distinct, differently-named facilities near each other are both kept.
            if same_name or d <= 80:
                dup = True
                break
        if dup:
            dropped += 1
        else:
            kept.append(el)
    print(f"dedupe: kept {len(kept)}, dropped {dropped}")

    # province + city + name + needsReview
    for el in kept:
        prov, inferred = province_of(el["lat"], el["lng"], provinces)
        el["province"] = prov
        el["cityCode"] = cmap.get(norm_name(prov or ""), None)
        el["district"] = district_hint(el)
        el["needsReview"] = False
        if inferred:
            el["needsReview"] = True
        el["_generated"] = False
        if not el["name"]:
            place = el["district"] or el["province"] or "TR"
            el["name"] = (f"Balıkçı Barınağı ({place})" if el["type"] == "FISHING_HARBOR"
                          else f"Marina ({place})")
            el["needsReview"] = True
            el["_generated"] = True

    # reconcile the interim 22 (OSM row wins coords/name, keeps the interim CODE so FK refs survive).
    # INTERIM-DRIVEN so the best OSM row wins each code — NAME agreement beats raw proximity. Priority:
    #   1) name match within 5 km   (fixes "Milta Bodrum Marina" at 331 m vs a nearer, differently-named
    #      neighbour like "Bodrum Yachting" at 227 m — name wins),
    #   2) distinctive name token (len>=5, not a province name) within 20 km (interim hand coords were
    #      approximate; avoids fusing different marinas in a city, e.g. "İzmir Levent" vs "İzmir Marina"),
    #   3) nearest within 300 m as a last resort (coords only, no name signal).
    # An OSM row already claimed by an earlier interim code can't be reused.
    interim = json.load(open(INTERIM))
    province_tokens = {strip_tr(c["name"]["tr"]).upper() for c in json.load(open(CITIES))}

    def distinctive(name):
        # norm_name already drops generic marina/harbour words; keep tokens >=4 chars that aren't a
        # province name — so "İzmir Marina" contributes NOTHING (province + generic) and can't false-match.
        return {t for t in norm_name(name).split() if len(t) >= 4 and t not in province_tokens}

    used_codes = set()
    interim_left = {}
    for hand in interim:
        ht = distinctive(hand["name"])
        avail = [el for el in kept if el["type"] == "MARINA" and not el.get("code")]
        def dist(el):
            return haversine_m(hand["latitude"], hand["longitude"], el["lat"], el["lng"])
        # score by shared distinctive tokens (name agreement); highest overlap wins, nearest breaks ties.
        # "Milta Bodrum Marina" shares {MILTA,BODRUM}=2 and beats "Bodrum Yachting" {BODRUM}=1 even though
        # the latter is nearer; "İzmir Levent Marina" {LEVENT} shares 0 with "İzmir Marina" -> no match.
        scored = [(len(ht & distinctive(el["name"])), dist(el), el) for el in avail]
        named = [s for s in scored if s[0] >= 1 and s[1] <= 20000]
        near = [s for s in scored if s[1] <= 300]
        pick = None
        if named:
            pick = min(named, key=lambda s: (-s[0], s[1]))[2]
        elif near:
            pick = min(near, key=lambda s: s[1])[2]
        if pick is not None:
            pick["code"] = hand["code"]
            used_codes.add(hand["code"])
            if dist(pick) > 300:  # coord/name shifted vs the hand entry — worth a glance
                pick["needsReview"] = True
        else:
            interim_left[hand["code"]] = hand

    # assign fresh slug codes to the rest (stable: disambiguate collisions by osmId).
    # Synthesised-name rows get a compact type+osmId code (no repeated province in the name slug).
    for el in kept:
        if el.get("code"):
            continue
        prov_slug = slugify(el["province"] or "TR", 12)
        if el.get("_generated"):
            code = f"TR_{prov_slug}_{'FISH' if el['type']=='FISHING_HARBOR' else 'MARINA'}_{el['osmId'].upper()}"
        else:
            base = f"TR_{prov_slug}_{slugify(el['name'], 30)}".strip("_")
            code = base if base not in used_codes else f"{base}_{el['osmId'].upper()}"
        used_codes.add(code)
        el["code"] = code

    # interim marinas NOT found in OSM: keep them (real marinas, FK-safe) flagged for review.
    for code, hand in interim_left.items():
        prov, _ = province_of(hand["latitude"], hand["longitude"], provinces)
        kept.append({
            "code": code, "name": hand["name"], "type": "MARINA",
            "province": prov, "cityCode": hand.get("cityCode") or cmap.get(norm_name(prov or ""), None),
            "district": None,
            "lat": hand["latitude"], "lng": hand["longitude"], "osmId": None,
            "needsReview": True, "tags": {},
        })
    print(f"reconcile: matched {len(used_codes & set(i['code'] for i in interim))} of {len(interim)} interim; "
          f"kept {len(interim_left)} interim-only (flagged)")

    # emit — the SEED file (repo) is a plain JSON array in the exact shape MarinaSeedModel reads
    # (case-insensitive keys; latitude/longitude, not lat/lng). Ordered by province then type then name.
    out = []
    for el in sorted(kept, key=lambda e: (e["province"] or "zz", e["type"], e["name"])):
        out.append({
            "code": el["code"],
            "name": el["name"],
            "type": el["type"],
            "countryCode": "TR",
            "cityCode": el.get("cityCode"),
            "province": el["province"],
            "district": el.get("district"),
            "latitude": round(el["lat"], 7),
            "longitude": round(el["lng"], 7),
            "osmId": el.get("osmId"),
            "needsReview": bool(el.get("needsReview")),
            "isActive": True,
        })

    with open(SEED_JSON, "w") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
    with open(SEED_ATTR, "w") as f:
        f.write(ATTRIBUTION + "\n\n"
                "marinas.json is derived from OpenStreetMap via tools/osm-marina-seed (Overpass).\n"
                "Keep this attribution wherever the dataset ships (ODbL share-alike).\n")

    # review twin (CSV) for humans
    with open(OUT_CSV, "w", newline="") as f:
        w = csv.writer(f)
        w.writerow(["# " + ATTRIBUTION])
        w.writerow(["code", "name", "type", "province", "district", "cityCode", "latitude", "longitude", "osmId", "needsReview"])
        for r in out:
            w.writerow([r["code"], r["name"], r["type"], r.get("province"), r.get("district"),
                        r.get("cityCode"), r["latitude"], r["longitude"], r.get("osmId"), r.get("needsReview")])

    # report
    from collections import Counter
    by_type = Counter(r["type"] for r in out)
    by_prov = Counter((r["province"] or "UNKNOWN") for r in out)
    print("\n=== COUNTS ===")
    print("total:", len(out))
    print("by type:", dict(by_type))
    print("needsReview:", sum(1 for r in out if r.get("needsReview")))
    print("with cityCode:", sum(1 for r in out if r.get("cityCode")))
    print("\nby province (top 25):")
    for p, n in by_prov.most_common(25):
        print(f"  {p:20s} {n}")
    print(f"\nwrote SEED {SEED_JSON}\nwrote {SEED_ATTR}\nwrote REVIEW {OUT_CSV}")


if __name__ == "__main__":
    main()
