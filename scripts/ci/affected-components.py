#!/usr/bin/env python3
"""Değişen dosyalardan HANGİ imajların yeniden derlenmesi gerektiğini hesaplar.

Neden dizin adına bakmak YETMEZ:
  Modules/Payment/src/Aizen.Modules.Payment.Abstraction içindeki bir değişiklik
  Profile modülünü de etkiler, çünkü Profile ona ProjectReference veriyor.
  "Modules/Payment değişti → payment derle" demek Profile'ı bayat bırakırdı.

Bu yüzden tüm .csproj dosyalarından gerçek referans grafiği çıkarılıyor ve her
bileşenin geçişli bağımlılık kümesi hesaplanıyor. Değişen bir proje, o projeye
(doğrudan ya da dolaylı) bağlı HER bileşeni tetikler.

Kullanım:
    git diff --name-only BASE HEAD | python3 scripts/ci/affected-components.py
Çıktı: stdout'a GitHub matris JSON'u, stderr'e insan okunur açıklama.
"""
import json, os, re, sys
from collections import defaultdict, deque

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# Bu yollardan biri değişirse HER ŞEY yeniden derlenir: ortak build tanımı,
# çözüm dosyası veya derleyiciyi etkileyen kök yapılandırma.
GLOBAL_TRIGGERS = (
    "build/Dockerfile", "build/components.json", ".dockerignore",
    ".github/workflows/images.yml", "Directory.Build.props", "Directory.Build.targets",
    "global.json", "NuGet.config", "nuget.config", "Aizen.sln",
)

def norm(p): return p.replace("\\", "/")

def load_graph():
    """csproj → doğrudan referans verdiği csproj'lar."""
    graph, projects = {}, []
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in ("bin", "obj", ".git", "node_modules")]
        for fn in filenames:
            if not fn.endswith(".csproj"):
                continue
            full = os.path.join(dirpath, fn)
            rel = norm(os.path.relpath(full, ROOT))
            projects.append(rel)
            try:
                text = open(full, encoding="utf-8", errors="ignore").read()
            except OSError:
                text = ""
            refs = set()
            for m in re.findall(r'Include="([^"]+\.csproj)"', text):
                target = os.path.normpath(os.path.join(dirpath, norm(m)))
                refs.add(norm(os.path.relpath(target, ROOT)))
            graph[rel] = refs
    return graph, projects

def closure(graph, start):
    """start projesinin geçişli bağımlılıkları (kendisi dahil)."""
    seen, q = {start}, deque([start])
    while q:
        cur = q.popleft()
        for dep in graph.get(cur, ()):
            if dep not in seen:
                seen.add(dep); q.append(dep)
    return seen

def owning_project(path, projects):
    """Dosyayı içeren en SPESİFİK projeyi bul (en uzun dizin öneki)."""
    best = None
    for proj in projects:
        d = os.path.dirname(proj) + "/"
        if path.startswith(d) and (best is None or len(d) > len(os.path.dirname(best) + "/")):
            best = proj
    return best

def main():
    changed = [norm(l.strip()) for l in sys.argv[1:] or sys.stdin if l.strip()]
    comps = json.load(open(os.path.join(ROOT, "build/components.json"), encoding="utf-8"))["components"]

    def emit(selected, reason):
        print(reason, file=sys.stderr)
        for c in selected:
            print(f"  → {c['id']}", file=sys.stderr)
        print(json.dumps({"include": selected}))
        return 0

    if not changed:
        return emit(comps, "Değişen dosya listesi boş → temkinli davranıp hepsi derleniyor.")

    for f in changed:
        if f in GLOBAL_TRIGGERS or f.endswith(("Directory.Build.props", "Directory.Build.targets")):
            return emit(comps, f"Ortak dosya değişti ({f}) → hepsi derleniyor.")

    graph, projects = load_graph()

    # Değişen dosyaları projelere eşle.
    changed_projects, ignored, unknown = set(), [], []
    for f in changed:
        if "/deploy/" in f or f.startswith("docs/") or f.startswith("infrastructure/") \
           or f.startswith("scripts/") or f.endswith(".md"):
            ignored.append(f); continue          # chart/doküman → imaj etkilenmez
        proj = owning_project(f, projects)
        if proj:
            changed_projects.add(proj)
        elif f.startswith(("Core/", "Modules/", "Bff/")):
            unknown.append(f)                     # proje dışı ama kaynak ağacında

    if unknown:
        return emit(comps, "Projeye eşlenemeyen kaynak dosyası var "
                           f"({unknown[0]}) → temkinli davranıp hepsi derleniyor.")
    if not changed_projects:
        print(f"Yalnız imajı etkilemeyen dosyalar değişti ({len(ignored)} adet) → derleme yok.",
              file=sys.stderr)
        print(json.dumps({"include": []}))
        return 0

    selected = []
    for c in comps:
        if closure(graph, norm(c["project"])) & changed_projects:
            selected.append(c)

    return emit(selected, f"{len(changed_projects)} proje değişti → {len(selected)} bileşen derleniyor.")

if __name__ == "__main__":
    sys.exit(main())
