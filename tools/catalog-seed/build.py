#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Catalog seed generator — turns the curated facts in catalog_data.py into the
four seed JSON files the CatalogJsonSeedService reads, plus CSV twins for human
review and a summary report.

    cd tools/catalog-seed
    python3 build.py

Emits →  Modules/ReferenceData/.../Seed/Json/Catalog/{vessel-brands,vessel-models,
         engine-brands,engine-models}.json               (overwrites the tiny samples)
      →  out/{vessel-brands,vessel-models,engine-brands,engine-models}.review.csv

No third-party deps (stdlib only). Codes are generated with the SAME algorithm
as the C# CatalogNameNormalizer.Slug so seeded codes match owner/admin-submitted
codes (upper-invariant, non-alnum runs → '_', collision → numeric suffix).
"""
import csv
import json
import os
import re
import sys

import catalog_data as data

HERE = os.path.dirname(os.path.abspath(__file__))
SEED_DIR = os.path.normpath(os.path.join(
    HERE, "..", "..",
    "Modules", "ReferenceData", "src",
    "Aizen.Modules.ReferenceData.Repository", "Seed", "Json", "Catalog"))
OUT_DIR = os.path.join(HERE, "out")


# ── name normalization — mirrors CatalogNameNormalizer (C#) ──
def clean(name: str) -> str:
    return re.sub(r"\s+", " ", (name or "").strip())


def key(name: str) -> str:
    # C# ToUpperInvariant(); Python str.upper() agrees for the chars we use.
    return clean(name).upper()


def slug(name: str) -> str:
    s = re.sub(r"[^A-Z0-9]+", "_", key(name)).strip("_")
    return s or "ITEM"


class CodeAllocator:
    """slug + numeric suffix on collision — matches the module's code assignment."""
    def __init__(self):
        self._seen = set()

    def take(self, name: str) -> str:
        base = slug(name)
        code = base
        n = 1
        while code in self._seen:
            n += 1
            code = f"{base}_{n}"
        self._seen.add(code)
        return code


def dedupe_key(*parts) -> str:
    return "|".join(key(p) for p in parts)


# ── build brands ──
def build_brands(rows, has_country):
    alloc = CodeAllocator()
    out, seen, dupes = [], {}, 0
    for row in rows:
        if has_country:
            name, country, source, review = row
        else:
            name, source, review = row
            country = None
        k = key(name)
        if k in seen:
            dupes += 1
            continue
        code = alloc.take(name)
        seen[k] = code
        rec = {"code": code, "name": clean(name)}
        if has_country:
            rec["countryCode"] = country
        rec["isActive"] = True
        rec["source"] = source
        rec["needsReview"] = bool(review)
        out.append(rec)
    return out, seen, dupes


# ── build models ──
def build_vessel_models(rows, brand_codes):
    per_brand_alloc = {}
    out, seen, dupes, orphan = [], set(), 0, 0
    for name, model, vtype, yf, yt, length, source, review in rows:
        bcode = brand_codes.get(key(name))
        if not bcode:
            orphan += 1
            print(f"  ! vessel model '{model}' → unknown brand '{name}' (skipped)", file=sys.stderr)
            continue
        dk = dedupe_key(name, model)
        if dk in seen:
            dupes += 1
            continue
        seen.add(dk)
        alloc = per_brand_alloc.setdefault(bcode, CodeAllocator())
        out.append({
            "brandCode": bcode,
            "code": alloc.take(model),
            "name": clean(model),
            "vesselTypeCode": vtype,
            "yearFrom": yf,
            "yearTo": yt,
            "lengthMeters": length,
            "isActive": True,
            "source": source,
            "needsReview": bool(review) or yf is None,
        })
    return out, dupes, orphan


def build_engine_models(rows, brand_codes):
    per_brand_alloc = {}
    out, seen, dupes, orphan = [], set(), 0, 0
    for name, model, hp, ftype, etype, yf, yt, source, review in rows:
        bcode = brand_codes.get(key(name))
        if not bcode:
            orphan += 1
            print(f"  ! engine model '{model}' → unknown brand '{name}' (skipped)", file=sys.stderr)
            continue
        dk = dedupe_key(name, model)
        if dk in seen:
            dupes += 1
            continue
        seen.add(dk)
        alloc = per_brand_alloc.setdefault(bcode, CodeAllocator())
        out.append({
            "brandCode": bcode,
            "code": alloc.take(model),
            "name": clean(model),
            "horsePower": hp,
            "fuelTypeCode": ftype,
            "engineTypeCode": etype,
            "yearFrom": yf,
            "yearTo": yt,
            "isActive": True,
            "source": source,
            "needsReview": bool(review) or hp is None,
        })
    return out, dupes, orphan


def write_json(path, rows):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(rows, f, ensure_ascii=False, indent=2)
        f.write("\n")


def write_csv(path, rows):
    if not rows:
        return
    os.makedirs(os.path.dirname(path), exist_ok=True)
    cols = list(rows[0].keys())
    with open(path, "w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=cols)
        w.writeheader()
        for r in rows:
            w.writerow(r)


def main():
    vbrands, vbmap, vbdup = build_brands(data.BOAT_BRANDS, has_country=True)
    ebrands, ebmap, ebdup = build_brands(data.ENGINE_BRANDS, has_country=False)
    vmodels, vmdup, vmorph = build_vessel_models(data.BOAT_MODELS, vbmap)
    emodels, emdup, emorph = build_engine_models(data.ENGINE_MODELS, ebmap)

    files = {
        "vessel-brands.json": vbrands,
        "vessel-models.json": vmodels,
        "engine-brands.json": ebrands,
        "engine-models.json": emodels,
    }
    for fname, rows in files.items():
        write_json(os.path.join(SEED_DIR, fname), rows)
        write_csv(os.path.join(OUT_DIR, fname.replace(".json", ".review.csv")), rows)

    # ── report ──
    print("catalog-seed build complete\n")
    print(f"seed dir : {SEED_DIR}")
    print(f"csv  dir : {OUT_DIR}\n")
    print(f"vessel brands : {len(vbrands):3d}  (dupes dropped {vbdup})")
    print(f"vessel models : {len(vmodels):3d}  (dupes {vmdup}, orphan {vmorph})")
    print(f"engine brands : {len(ebrands):3d}  (dupes dropped {ebdup})")
    print(f"engine models : {len(emodels):3d}  (dupes {emdup}, orphan {emorph})")

    vreview = sum(1 for r in vmodels if r["needsReview"])
    ereview = sum(1 for r in emodels if r["needsReview"])
    print(f"\nneedsReview: vessel-models {vreview}/{len(vmodels)}, "
          f"engine-models {ereview}/{len(emodels)} "
          f"(mostly undocumented yearFrom / hp — v1 gaps)")

    # models per brand
    print("\nvessel models per brand:")
    counts = {}
    codeToName = {r["code"]: r["name"] for r in vbrands}
    for r in vmodels:
        counts[r["brandCode"]] = counts.get(r["brandCode"], 0) + 1
    for code in sorted(counts, key=lambda c: (-counts[c], c)):
        print(f"  {codeToName.get(code, code):22s} {counts[code]}")

    print("\nengine models per brand:")
    counts = {}
    codeToName = {r["code"]: r["name"] for r in ebrands}
    for r in emodels:
        counts[r["brandCode"]] = counts.get(r["brandCode"], 0) + 1
    for code in sorted(counts, key=lambda c: (-counts[c], c)):
        print(f"  {codeToName.get(code, code):22s} {counts[code]}")


if __name__ == "__main__":
    main()
