#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Yer-tutucu denetimi — doldurulmamış yapılandırma tuzağını yakalar.

Bff/src ve Modules/*/src altındaki her appsettings*.json dosyasını gezer; değeri "__FROM_ENV__" veya
"__FROM_SECRET__" olan her ayarı bulur; JSON yolunu ortam-değişkeni adına çevirir (a:b -> a__b); projeyi
build/components.json üzerinden chart'ına eşler; ve bu adın chart'ın values dosyalarında geçtiğini doğrular.
Eksik varsa 1 ile çıkar.

Gerçek vaka: MarineProviderKeycloak__VerifyEmailRedirectUri doldurulmamıştı → Keycloak'a redirect_uri=__FROM_ENV__
gitti → 400 → doğrulama e-postası hiç gönderilmedi → ekran "Kayıt başarılı" dedi.
"""
import glob
import json
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PLACEHOLDERS = ("__FROM_ENV__", "__FROM_SECRET__")


def load_components():
    with open(os.path.join(ROOT, "build", "components.json"), encoding="utf-8") as f:
        data = json.load(f)
    comps = []
    for c in data.get("components", []):
        proj = c.get("project")
        chart = c.get("chart")
        if not proj or not chart:
            continue
        comps.append((os.path.dirname(proj).replace("\\", "/"), chart, c.get("id", "?")))
    return comps


def find_component(rel_path, comps):
    """appsettings yolunu, project dizini onun atası olan en spesifik (en uzun) bileşene eşler."""
    best = None
    rel = rel_path.replace("\\", "/")
    for proj_dir, chart, cid in comps:
        if rel.startswith(proj_dir + "/"):
            if best is None or len(proj_dir) > len(best[0]):
                best = (proj_dir, chart, cid)
    return best


def collect_placeholders(node, path, out):
    if isinstance(node, dict):
        for k, v in node.items():
            collect_placeholders(v, path + [k], out)
    elif isinstance(node, list):
        for i, v in enumerate(node):
            collect_placeholders(v, path + [str(i)], out)
    elif isinstance(node, str) and node in PLACEHOLDERS:
        out.append(("__".join(path), node))  # a:b:c -> a__b__c (.NET ortam değişkeni kuralı)


def appsettings_files():
    files = []
    for pattern in ("Bff/src/**/appsettings*.json", "Modules/*/src/**/appsettings*.json"):
        for f in glob.glob(os.path.join(ROOT, pattern), recursive=True):
            norm = f.replace("\\", "/")
            if "/bin/" in norm or "/obj/" in norm:
                continue
            files.append(f)
    return sorted(set(files))


def main():
    comps = load_components()
    failures = []
    skipped = []
    scanned = 0

    for f in appsettings_files():
        rel = os.path.relpath(f, ROOT).replace("\\", "/")
        try:
            with open(f, encoding="utf-8") as fh:
                data = json.load(fh)
        except (json.JSONDecodeError, OSError) as ex:
            failures.append((rel, f"(okunamadı/parse edilemedi: {ex})", "", ""))
            continue

        placeholders = []
        collect_placeholders(data, [], placeholders)
        if not placeholders:
            continue
        scanned += 1

        comp = find_component(rel, comps)
        if comp is None:
            skipped.append(rel)
            continue

        _, chart, cid = comp
        values_files = glob.glob(os.path.join(ROOT, chart, "values*.yaml"))
        values_text = ""
        for vf in values_files:
            with open(vf, encoding="utf-8") as vfh:
                values_text += vfh.read() + "\n"

        for name, kind in sorted(set(placeholders)):
            if name not in values_text:
                failures.append((rel, name, kind, chart))

    print(f"Denetlenen (yer-tutucu içeren) appsettings dosyası: {scanned}")
    if skipped:
        print("UYARI: components.json ile eşleşmeyen dosyalar (atlandı):")
        for s in skipped:
            print(f"  - {s}")

    if failures:
        print("\n🔴 DOLDURULMAMIŞ yer-tutucular (ortam-değişkeni adı chart values'ta YOK):")
        for rel, name, kind, chart in failures:
            print(f"  [{kind}] {name}   ({rel}  →  {chart})")
        print(f"\nToplam {len(failures)} doldurulmamış. values-dev.yaml VE values-prod.yaml'a ekleyin.")
        return 1

    print("\n✅ Tüm yer-tutucuların chart values'ta karşılığı var.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
