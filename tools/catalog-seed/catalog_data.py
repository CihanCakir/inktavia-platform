# -*- coding: utf-8 -*-
"""
Curated TR-weighted boat + engine catalog (v1.1) — COMPILED FACTS, per-row sourced.

v1.1 expands model LINES into the real, selectable NAMED models (e.g. the
"Beneteau Oceanis" line → Oceanis 30.1 / 34.1 / 37.1 / 40.1 / 46.1 / 51.1 …), and
each engine FAMILY into its HP variants (the selection point for engines).

Rules (see README):
  * We compile FACTS (brand / model / production years / hp / fuel) from
    manufacturers' own current + archived model pages. We do NOT bulk-copy any
    single protected DB. Every row carries a `source` = the manufacturer site.
  * `year_from` is set ONLY where the production-start year is confidently known;
    otherwise None → build.py auto-flags the row `needsReview` (v1 gap). Because
    most expanded models are enumerated by the manufacturer's naming convention
    without a confirmed launch year, they ship `needsReview=true` for the admin
    queue to confirm — this is intentional and honest.
  * Engine `hp` is REQUIRED (non-null) — it is the selection point. Fractional-HP
    outboards (F9.9, DF2.5 …) keep the exact name but store rounded HP + review.
  * `vtype`/`ftype`/`etype` are BEST-GUESS coarse codes — reconcile to the real
    MARINE lookup vocabulary before they drive UI filters (see README).

Final flat lists consumed by build.py:
  BOAT_BRANDS  : (name, country, source, review)
  BOAT_MODELS  : (brand_name, model, vtype, year_from, year_to, length_m, source, review)
  ENGINE_BRANDS: (name, source, review)
  ENGINE_MODELS: (brand_name, model, hp, ftype, etype, year_from, year_to, source, review)
"""

# ───────────────────────── build helpers ─────────────────────────

BOAT_MODELS = []
ENGINE_MODELS = []


def V(brand, vtype, source, entries, review=False):
    """Append vessel models. entry = "Name" | ("Name", length_m) | ("Name", length_m, year_from)."""
    for e in entries:
        if isinstance(e, str):
            name, length, yf = e, None, None
        elif len(e) == 2:
            name, length = e
            yf = None
        else:
            name, length, yf = e
        BOAT_MODELS.append((brand, name, vtype, yf, None, length, source, review))


def L(prefix, nums, sep=""):
    """Number-suffixed model names, e.g. L("Oceanis ", [40.1, 46.1]) → ['Oceanis 40.1', ...]."""
    return [f"{prefix}{sep}{n}" for n in nums]


def _hp(p):
    f = float(p)
    return (int(f), False) if f.is_integer() else (round(f), True)


def E(brand, ftype, etype, source, prefix, hps, sep="", review=False):
    """Engine family expanded by HP. Model name = prefix+sep+hp; HP parsed from the value."""
    for p in hps:
        hp, frac = _hp(p)
        ENGINE_MODELS.append((brand, f"{prefix}{sep}{p}", hp, ftype, etype, None, None, source, review or frac))


def EL(brand, ftype, etype, source, pairs, review=False):
    """Engine models with explicit (name, hp) pairs (name ≠ prefix+hp)."""
    for name, hp in pairs:
        ENGINE_MODELS.append((brand, name, int(round(hp)), ftype, etype, None, None, source, review))


# ───────────────────────────── BOAT BRANDS ─────────────────────────────

BOAT_BRANDS = [
    # ── Turkish builders ──
    ("Sirena Yachts",   "TR", "https://www.sirenayachts.com/",        False),
    ("Numarine",        "TR", "https://www.numarine.com/",            False),
    ("Vicem Yachts",    "TR", "https://www.vicemyachts.com/",         False),
    ("Mengi Yay",       "TR", "https://www.mengiyay.com/",            False),
    ("Bering Yachts",   "TR", "https://beringyachts.com/",            False),
    ("Mazu Yachts",     "TR", "https://www.mazuyachts.com/",          False),
    ("Sarp Yachts",     "TR", "https://www.sarpyachts.com/",          True),
    ("Bilgin Yachts",   "TR", "https://www.bilginyacht.com/",         True),
    ("Turquoise Yachts","TR", "https://www.turquoiseyachts.com/",     True),
    ("Tansu Yachts",    "TR", "https://www.tansuyachts.com/",         True),
    ("Su Marine Yachts","TR", "https://www.sumarineyachts.com/",      True),
    ("Alesta Yachts",   "TR", "https://www.alestayacht.com/",         True),
    # ── Global sail majors ──
    ("Beneteau",        "FR", "https://www.beneteau.com/",            False),
    ("Jeanneau",        "FR", "https://www.jeanneau.com/",            False),
    ("Bavaria Yachts",  "DE", "https://www.bavariayachts.com/",       False),
    ("Hanse",           "DE", "https://www.hanseyachts.com/",         False),
    ("Dufour",          "FR", "https://www.dufour-yachts.com/",       False),
    ("Elan",            "SI", "https://elan-yachts.com/",             False),
    ("X-Yachts",        "DK", "https://www.x-yachts.com/",            False),
    ("Grand Soleil",    "IT", "https://www.grandsoleil.net/",         False),
    ("Hallberg-Rassy",  "SE", "https://www.hallberg-rassy.com/",      True),
    ("Amel",            "FR", "https://www.amel.fr/",                 True),
    # ── Catamarans ──
    ("Lagoon",          "FR", "https://www.cata-lagoon.com/",         False),
    ("Fountaine Pajot", "FR", "https://www.catamarans-fountaine-pajot.com/", False),
    ("Bali Catamarans", "FR", "https://www.bali-catamarans.com/",     False),
    ("Excess Catamarans","FR","https://www.excess-catamarans.com/",   False),
    ("Nautitech",       "FR", "https://www.nautitechcatamarans.com/", False),
    ("Leopard Catamarans","ZA","https://www.leopardcatamarans.com/",  False),
    ("Sunreef Yachts",  "PL", "https://www.sunreef-yachts.com/",      True),
    # ── Motor / flybridge majors ──
    ("Azimut Yachts",   "IT", "https://www.azimutyachts.com/",        False),
    ("Ferretti Yachts", "IT", "https://www.ferretti-yachts.com/",     False),
    ("Princess Yachts", "GB", "https://www.princessyachts.com/",      False),
    ("Sunseeker",       "GB", "https://www.sunseeker.com/",           False),
    ("Fairline",        "GB", "https://www.fairline.com/",            False),
    ("Sealine",         "DE", "https://www.sealine.com/",             False),
    ("Galeon",          "PL", "https://www.galeonyachts.com/",        False),
    ("Prestige Yachts", "FR", "https://www.prestige-yachts.com/",     False),
    ("Absolute Yachts", "IT", "https://www.absoluteyachts.com/",      False),
    ("Cranchi",         "IT", "https://www.cranchi.com/",             False),
    ("Sessa Marine",    "IT", "https://www.sessamarine.com/",         True),
    ("Fjord",           "NO", "https://www.fjordboats.com/",          False),
    ("Nimbus",          "SE", "https://www.nimbus.se/",               False),
    ("Riva",            "IT", "https://www.riva-yacht.com/",          False),
    ("Sanlorenzo",      "IT", "https://www.sanlorenzoyacht.com/",     True),
    # ── Dayboats / bowriders / center-consoles ──
    ("Sea Ray",         "US", "https://www.searay.com/",              False),
    ("Bayliner",        "US", "https://www.bayliner.com/",            False),
    ("Quicksilver",     "FR", "https://www.quicksilver-boats.com/",   False),
    ("Boston Whaler",   "US", "https://www.bostonwhaler.com/",        False),
    ("Axopar",          "FI", "https://www.axopar.com/",              False),
    ("Saxdor Yachts",   "FI", "https://www.saxdoryachts.com/",        False),
    ("De Antonio Yachts","ES","https://www.deantonioyachts.com/",     False),
    ("Invictus Yacht",  "IT", "https://www.invictusyacht.com/",       False),
    ("Karnic",          "CY", "https://www.karnic.eu/",               True),
    ("Parker Poland",   "PL", "https://www.parkerpoland.com/",        True),
    # ── RIBs ──
    ("Zodiac",          "FR", "https://www.zodiac-nautic.com/",       False),
    ("Grand Marine",    "US", "https://www.grandmarine.com/",         True),
    ("BRIG",            "UA", "https://www.brig-boats.com/",          True),
    ("Highfield Boats", "HK", "https://www.highfieldboats.com/",      True),
    ("Nuova Jolly",     "IT", "https://www.nuovajollymarine.com/",    True),
    # ── PWC (jet-ski) ──
    ("Sea-Doo",         "CA", "https://www.sea-doo.com/",             False),
    ("Yamaha WaveRunner","JP","https://www.yamahawaverunners.com/",   False),
    ("Kawasaki Jet Ski","JP", "https://www.kawasaki.com/en-us/personal-watercraft", True),
]


# ───────────────────────────── BOAT MODELS ─────────────────────────────
# Confirmed launch years/lengths are given where known; otherwise year is left
# null → the row auto-flags needsReview for the admin queue to confirm.

# ── Turkish builders ──
V("Sirena Yachts", "MOTORYACHT", "https://www.sirenayachts.com/", [
    ("Sirena 48", 14.7, 2019), ("Sirena 58", 18.0, 2016), ("Sirena 68", 21.0, 2018),
    ("Sirena 78", 24.0, 2022), ("Sirena 88", 27.0, 2021),
])
V("Numarine", "MOTORYACHT", "https://www.numarine.com/", [
    ("22XP", 22.0, 2018), ("26XP", 26.0, 2016), ("32XP", 32.0, 2019), ("37XP", 37.0, 2021),
    "62 Flybridge", "70 Flybridge", "78 Flybridge", "105 Hardtop", "26XP Hybrid",
])
V("Vicem Yachts", "MOTORYACHT", "https://www.vicemyachts.com/", [
    "Vicem Classic 46", "Vicem Classic 58", "Vicem Classic 78", "Vicem Cruiser 92",
    "Vicem Bahama Bay 34", "Vicem Vulcan 32M", "Vicem 107 Cruiser", "Vicem 46 IPS",
])
V("Mengi Yay", "MOTORYACHT", "https://www.mengiyay.com/", [
    "Bandido 66", "Bandido 75", "Bandido 90", ("Bandido 148", 45.0), "Trawler 78", "Trawler 90",
])
V("Bering Yachts", "MOTORYACHT", "https://beringyachts.com/", [
    "Bering 55", "Bering 60", "Bering 65", "Bering 70", "Bering 77", ("Bering 92", 28.0),
    "Bering 80", "Bering 145",
])
V("Mazu Yachts", "MOTORYACHT", "https://www.mazuyachts.com/", [
    ("Mazu 38", 11.7), ("Mazu 42", 13.0), ("Mazu 52", 16.0), ("Mazu 82", 25.0),
])
V("Sarp Yachts", "MOTORYACHT", "https://www.sarpyachts.com/", [
    "Sarp XSR 85", "Sarp XSR 105", "Sarp 46", "Sarp 58",
])
V("Bilgin Yachts", "SUPERYACHT", "https://www.bilginyacht.com/", [
    "Bilgin 156", "Bilgin 163", "Bilgin 197", "Bilgin 263", "Bilgin Classic 160",
])
V("Turquoise Yachts", "SUPERYACHT", "https://www.turquoiseyachts.com/", [
    "Turquoise 55m", "Turquoise 66m", "Turquoise 77m",
])
V("Tansu Yachts", "MOTORYACHT", "https://www.tansuyachts.com/", [
    "Tansu 42m", "Tansu Novia", "Tansu So'Mar",
])
V("Su Marine Yachts", "MOTORYACHT", "https://www.sumarineyachts.com/", [
    "Su 11", "Su 12", "Su 15", "Su Barca 45",
])
V("Alesta Yachts", "MOTORYACHT", "https://www.alestayacht.com/", [
    "Alesta 34", "Alesta 40", "Alesta Explorer 48",
])

# ── Beneteau ──
_bene = "https://www.beneteau.com/"
V("Beneteau", "SAILBOAT", _bene, L("Oceanis ", ["30.1", "34.1", "37.1", "40.1", "46.1", "51.1"]))
V("Beneteau", "SAILBOAT", _bene, L("Oceanis Yacht ", [54, 60, 62]))
V("Beneteau", "SAILBOAT", _bene, L("Oceanis ", [31, 35, 37, 38, 41, 43, 45, 48, 50, 55, 58]))  # archived
V("Beneteau", "SAILBOAT", _bene, ["First 14", "First 18", "First 24", "First 27", "First 36", "First 44", "First 53"])
V("Beneteau", "SAILBOAT", _bene, L("First ", [210, 235, 260, 285, 310, 375, 405, 456]))  # archived
V("Beneteau", "MOTORYACHT", _bene, L("Antares ", [7, 8, 9, 11, 12]))
V("Beneteau", "MOTORYACHT", _bene, L("Gran Turismo ", [32, 36, 41, 45]))
V("Beneteau", "MOTORYACHT", _bene, L("Swift Trawler ", [35, 41, 44, 48, 54]))
V("Beneteau", "MOTORYACHT", _bene, ["Flyer 6", "Flyer 7", "Flyer 8", "Flyer 9"])
V("Beneteau", "MOTORYACHT", _bene, L("Barracuda ", [7, 8, 9]))

# ── Jeanneau ──
_jean = "https://www.jeanneau.com/"
V("Jeanneau", "SAILBOAT", _jean, L("Sun Odyssey ", [350, 380, 410, 440, 490]))
V("Jeanneau", "SAILBOAT", _jean, L("Sun Odyssey ", [319, 349, 389, 419, 449, 479, 509, 519, 539]))  # archived
V("Jeanneau", "SAILBOAT", _jean, ["Sun Fast 30 OD", "Sun Fast 3300", "Sun Fast 3600", "Sun Fast 3200"])
V("Jeanneau", "SAILBOAT", _jean, L("Jeanneau Yachts ", [55, 60, 65]))
V("Jeanneau", "MOTORYACHT", _jean, L("Merry Fisher ", [605, 695, 795, 895, 1095, 1295]))
V("Jeanneau", "MOTORYACHT", _jean, ["Cap Camarat 5.5", "Cap Camarat 6.5", "Cap Camarat 7.5", "Cap Camarat 9.0", "Cap Camarat 10.5"])
V("Jeanneau", "MOTORYACHT", _jean, L("Leader ", [30, 33, 36, 40, 46]))
V("Jeanneau", "MOTORYACHT", _jean, ["NC 895", "NC 1095", "NC 37", "DB 37", "DB 43"])

# ── Bavaria ──
_bav = "https://www.bavariayachts.com/"
V("Bavaria Yachts", "SAILBOAT", _bav, L("Cruiser ", [34, 37, 41, 46, 51]))
V("Bavaria Yachts", "SAILBOAT", _bav, L("C", [38, 42, 45, 50, 57], sep=""))
V("Bavaria Yachts", "SAILBOAT", _bav, L("Bavaria ", [32, 36, 40, 44, 49], sep=""))  # archived
V("Bavaria Yachts", "MOTORYACHT", _bav, ["SR33", "SR36", "SR41"])
V("Bavaria Yachts", "MOTORYACHT", _bav, L("Vida ", [33]))
V("Bavaria Yachts", "MOTORYACHT", _bav, L("Bavaria Virtess ", [420]))

# ── Hanse ──
_hanse = "https://www.hanseyachts.com/"
V("Hanse", "SAILBOAT", _hanse, [("Hanse 348", 10.4), ("Hanse 388", 11.5), ("Hanse 418", 12.4),
                                ("Hanse 460", 14.0), ("Hanse 510", 15.0), ("Hanse 548", 16.4),
                                "Hanse 315", "Hanse 385", "Hanse 415", "Hanse 455", "Hanse 505", "Hanse 588", "Hanse 675"])

# ── Dufour ──
_duf = "https://www.dufour-yachts.com/"
V("Dufour", "SAILBOAT", _duf, [("Dufour 37", 11.2, 2022), ("Dufour 390", 11.9), ("Dufour 430", 12.8, 2019),
                               ("Dufour 470", 14.1, 2021), ("Dufour 530", 15.8, 2020), ("Dufour 61", 18.6),
                               "Dufour 310", "Dufour 350", "Dufour 360", "Dufour 382", "Dufour 412",
                               "Dufour 460", "Dufour 512", "Dufour 520", "Dufour 560"])

# ── Elan ──
V("Elan", "SAILBOAT", "https://elan-yachts.com/", [
    "Impression 40.1", "Impression 43", "Impression 45.1", "Impression 50.1",
    "E4", "E5", "E6", "GT5", "GT6", "GT6 Explorer", "S4", "S5",
])
# ── X-Yachts ──
V("X-Yachts", "SAILBOAT", "https://www.x-yachts.com/", [
    "Xc 38", "Xc 42", "Xp 44", "X4^0", "X4^3", "X4^6", "X4^9", "X5^6", "X6^5",
    "Xr 41", "X-Yachts X-43", "X-Yachts X-46",
])
# ── Grand Soleil ──
V("Grand Soleil", "SAILBOAT", "https://www.grandsoleil.net/", [
    ("Grand Soleil 34", None), ("Grand Soleil 40", 12.0), ("Grand Soleil 44", None),
    ("Grand Soleil 52", 15.8), "Grand Soleil 42 LC", "Grand Soleil 46 LC", "Grand Soleil 58",
    "Grand Soleil 65 LC", "Grand Soleil 72",
])
# ── Hallberg-Rassy ──
V("Hallberg-Rassy", "SAILBOAT", "https://www.hallberg-rassy.com/", [
    ("HR 340", 10.4), ("HR 40C", None), ("HR 40", 12.4), ("HR 44", 13.7), ("HR 50", 15.4),
    ("HR 57", 17.5), "HR 310", "HR 372", "HR 412", "HR 400",
])
# ── Amel ──
V("Amel", "SAILBOAT", "https://www.amel.fr/", [
    ("Amel 50", 15.4, 2017), ("Amel 60", 18.7, 2019), "Amel 54", "Amel 55", "Amel 64",
])

# ── Catamarans ──
_lag = "https://www.cata-lagoon.com/"
V("Lagoon", "CATAMARAN", _lag, [("Lagoon 40", 11.7, 2018), ("Lagoon 42", 12.8, 2016),
                                ("Lagoon 46", 13.9, 2019), ("Lagoon 50", 14.8, 2017),
                                ("Lagoon 55", 16.6, 2022), ("Lagoon 51", 15.6), ("Lagoon 60", 18.0),
                                "Lagoon 380", "Lagoon 400", "Lagoon 410", "Lagoon 421", "Lagoon 440",
                                "Lagoon 450", "Lagoon 52", "Lagoon 620", "Lagoon SEVENTY 7", "Lagoon SIXTY 5"])
_fp = "https://www.catamarans-fountaine-pajot.com/"
V("Fountaine Pajot", "CATAMARAN", _fp, [("Isla 40", 11.9, 2021), ("Elba 45", 13.7, 2019),
                                        ("Saba 50", 15.4, 2016), ("Aura 51", 15.4, 2021),
                                        ("Samana 59", 17.7), ("Tanna 47", 13.9), ("Astréa 42", 12.6),
                                        "Lucia 40", "Lipari 41", "Helia 44", "Saona 47", "Ipanema 58",
                                        "Alegria 67", "Thira 80"])
V("Bali Catamarans", "CATAMARAN", "https://www.bali-catamarans.com/", [
    ("Bali 4.2", 12.9, 2021), ("Bali 4.4", 13.3), ("Bali 4.6", 14.0, 2021), ("Bali 4.8", 14.7),
    ("Bali 5.4", 16.8, 2018), ("Catspace", 12.4, 2020), "Bali 4.0", "Bali 4.1", "Bali 4.3",
    "Bali 4.5", "Bali 5.8", "Bali Catspace Voile"])
V("Excess Catamarans", "CATAMARAN", "https://www.excess-catamarans.com/", [
    ("Excess 11", 11.3, 2019), ("Excess 12", 11.7), ("Excess 14", 13.7, 2021),
    ("Excess 15", 14.6), "Excess 13"])
V("Nautitech", "CATAMARAN", "https://www.nautitechcatamarans.com/", [
    ("Nautitech 40", 11.9), ("Nautitech 44", 13.1), ("Nautitech 46", 13.8),
    ("Nautitech 48", 14.5), "Nautitech 40 Open", "Nautitech 44 Open", "Nautitech 47", "Nautitech 54"])
V("Leopard Catamarans", "CATAMARAN", "https://www.leopardcatamarans.com/", [
    ("Leopard 40", 11.9), ("Leopard 42", 12.7, 2020), ("Leopard 45", 13.7, 2017),
    ("Leopard 46", 14.2), ("Leopard 50", 15.0, 2018), ("Leopard 53", 16.1),
    "Leopard 39", "Leopard 43", "Leopard 44", "Leopard 48", "Leopard 51 PC", "Leopard 53 PC"])
V("Sunreef Yachts", "CATAMARAN", "https://www.sunreef-yachts.com/", [
    ("Sunreef 60", 18.0), ("Sunreef 70", 21.0), ("Sunreef 80", 24.0), "Sunreef 50",
    "Sunreef 100", "Sunreef 43M", "Sunreef 60 Power", "Sunreef 70 Power", "Sunreef 80 Power",
    "Sunreef 40 Ultima"])

# ── Motor / flybridge majors ──
_azi = "https://www.azimutyachts.com/"
V("Azimut Yachts", "MOTORYACHT", _azi, L("Atlantis ", [45, 51])
  + L("Magellano ", [25, 30, 43, 60, 66])
  + ["Flybridge 53", "Flybridge 60", "Flybridge 68", "Flybridge 72", "Flybridge 78"]
  + ["S6", "S7", "S8", "S10"] + ["Seadeck 6", "Seadeck 7"])
V("Azimut Yachts", "SUPERYACHT", _azi, ["Grande 26M", "Grande 30M", "Grande 32M", "Grande 35M", "Grande 44M", "Grande Trideck"])
_fer = "https://www.ferretti-yachts.com/"
V("Ferretti Yachts", "MOTORYACHT", _fer, [("Ferretti 500", 15.5), ("Ferretti 580", 18.0),
                                          ("Ferretti 670", 20.7), ("Ferretti 720", 22.0),
                                          ("Ferretti 860", 26.4), ("Ferretti 1000", 30.0),
                                          "Ferretti 450", "Ferretti 550", "Ferretti 780",
                                          "Ferretti INFYNITO 90", "Ferretti INFYNITO 80"])
_pri = "https://www.princessyachts.com/"
V("Princess Yachts", "MOTORYACHT", _pri, L("V", [40, 50, 55, 60, 65, 78, 80])
  + L("F", [45, 50, 55, 58, 62, 70])
  + L("S", [62, 65, 72, 78, 80]) + L("Y", [72, 78, 80, 85, 95])
  + ["X80", "X95", "R35", "V78"])
V("Princess Yachts", "SUPERYACHT", _pri, L("Princess ", [30, 32, 35, 40], sep="M"))
_sun = "https://www.sunseeker.com/"
V("Sunseeker", "MOTORYACHT", _sun, L("Manhattan ", [55, 66, 68])
  + L("Predator ", [50, 55, 60, 65, 74])
  + L("Portofino ", [40, 47, 53]) + L("Camargue ", [50, 55])
  + ["Ocean 156", "Ocean 182", "Superhawk 55", "Hawk 38", "65 Sport Yacht", "76 Yacht", "88 Yacht", "90 Ocean", "100 Yacht"])
_fai = "https://www.fairline.com/"
V("Fairline", "MOTORYACHT", _fai, L("Targa ", [38, 40, 43, 45, 50, 58, 63, 65])
  + L("Squadron ", [50, 58, 68]) + L("Phantom ", [65]) + ["F//LINE 33", "F//LINE 65", "Targa 33"])
_sea = "https://www.sealine.com/"
V("Sealine", "MOTORYACHT", _sea, L("S", [330, 335, 430, 450]) + L("C", [330, 335, 390, 430, 450])
  + L("F", [430, 530]) + ["Sealine SC35", "Sealine SC42"])
_gal = "https://www.galeonyachts.com/"
V("Galeon", "MOTORYACHT", _gal, L("Galeon ", [325, 335, 375, 405, 440, 460, 500, 510, 560, 640, 680, 800])
  + ["Galeon 300", "Galeon 360", "Galeon 425", "Galeon 485", "Galeon 700"])
_pre = "https://www.prestige-yachts.com/"
V("Prestige Yachts", "MOTORYACHT", _pre, L("Prestige ", [420, 460, 520, 590, 690])
  + ["Prestige M48", "Prestige M8", "Prestige X60", "Prestige X70", "Prestige 400", "Prestige 450",
     "Prestige 500", "Prestige 630", "Prestige 750"])
_abs = "https://www.absoluteyachts.com/"
V("Absolute Yachts", "MOTORYACHT", _abs, L("Navetta ", [48, 52, 58, 64, 68, 73])
  + ["52 Fly", "56 Fly", "60 Fly", "62 Fly", "Absolute 47 Coupé", "Absolute 52 Coupé", "Absolute 58 Fly"])
_cra = "https://www.cranchi.com/"
V("Cranchi", "MOTORYACHT", _cra, [("Cranchi E26", 8.4), ("Cranchi A46", 14.5), "Cranchi E30",
                                  "Cranchi E52", "Cranchi Z35", "Cranchi T36", "Cranchi T43",
                                  "Cranchi Settantotto", "Cranchi Sessantasette", "Cranchi M44"])
V("Sessa Marine", "MOTORYACHT", "https://www.sessamarine.com/", [
    "Key Largo 24", "Key Largo 27", "Key Largo 34", "C38", "C42", "C47", "Fly 42", "Fly 47", "Fly 54"])
V("Fjord", "MOTORYACHT", "https://www.fjordboats.com/", [
    ("Fjord 41", 12.6), ("Fjord 44", 13.7), ("Fjord 48", 14.9), ("Fjord 53", 16.4),
    "Fjord 38", "Fjord 40", "Fjord 52", "Fjord 64"])
V("Nimbus", "MOTORYACHT", "https://www.nimbus.se/", L("Nimbus T", [8, 9, 11], sep="")
  + L("Nimbus C", [8, 9, 11], sep="") + L("Nimbus W", [9, 11], sep="")
  + ["Nimbus 305 Coupé", "Nimbus 405 Coupé", "Nimbus 465 Coupé"])
V("Riva", "MOTORYACHT", "https://www.riva-yacht.com/", [
    ("Aquariva", 10.1), ("Rivamare", 11.9), ("Iseo", 8.2), "Dolceriva", "Riva 68 Diable",
    "Riva 76 Perseo", "Riva 88 Folgore", "Riva 90 Argo", "Riva 100 Corsaro", "Riva 130 Bellissima"])
V("Sanlorenzo", "SUPERYACHT", "https://www.sanlorenzoyacht.com/", [
    "SL78", "SL86", "SL90A", "SL96A", "SL102A", "SL106", "SL120", "SX76", "SX88", "SX100",
    "SD90", "SD96", "SD118", "SP110"])

# ── Dayboats / bowriders / center-consoles ──
_sr = "https://www.searay.com/"
V("Sea Ray", "MOTORYACHT", _sr, L("SPX ", [190, 210, 230])
  + L("SLX ", [230, 260, 280, 310, 350, 400])
  + L("SDX ", [250, 270, 290])
  + L("Sundancer ", [265, 290, 320, 370, 400])
  + ["Sundancer 350 Coupe", "SLX-R 400e", "Sundancer 320", "Sundancer 350", "SL3", "230 SPX OB"])
_bay = "https://www.bayliner.com/"
V("Bayliner", "MOTORYACHT", _bay, L("VR", [4, 5, 6], sep="")
  + ["DX2000", "DX2050", "Element E16", "Element E18", "Element M15", "Element M17",
     "Ciera 8", "Trophy T20", "Trophy T22", "Trophy T24", "M15", "M19"])
_qs = "https://www.quicksilver-boats.com/"
V("Quicksilver", "MOTORYACHT", _qs, L("Activ ", [455, 505, 555, 605, 675, 755, 805, 875])
  + L("Captur ", [500, 555, 605, 625, 675, 755, 875])
  + ["Pilothouse 555", "Pilothouse 605", "Pilothouse 705", "Pilothouse 805"])
_bw = "https://www.bostonwhaler.com/"
V("Boston Whaler", "MOTORYACHT", _bw, L("Montauk ", [150, 170, 190, 210])
  + L("Outrage ", [220, 250, 280, 320, 330, 350, 380, 420])
  + L("Dauntless ", [170, 210, 230, 270])
  + L("Vantage ", [230, 270, 320])
  + L("Conquest ", [285, 325, 355]))
V("Axopar", "MOTORYACHT", "https://www.axopar.com/", [
    ("Axopar 22", 6.9, 2020), ("Axopar 25", 7.9, 2022), ("Axopar 28", 9.0, 2014),
    ("Axopar 37", 11.8, 2020), ("Axopar 45", 13.7, 2022), "Axopar 29", "Axopar 24"])
V("Saxdor Yachts", "MOTORYACHT", "https://www.saxdoryachts.com/", [
    ("Saxdor 200", 6.4, 2020), ("Saxdor 270", 8.3, 2021), ("Saxdor 320", 9.8, 2022),
    ("Saxdor 400", 11.9, 2023), "Saxdor 205", "Saxdor 340", "Saxdor 470"])
V("De Antonio Yachts", "MOTORYACHT", "https://www.deantonioyachts.com/", [
    ("D28", 8.5), ("D32", 9.9), ("D36", 11.0), ("D42", 12.6), ("D50", 15.0),
    "D29", "D23", "D60"])
V("Invictus Yacht", "MOTORYACHT", "https://www.invictusyacht.com/", [
    "GT320", "GT370", "GT420", "GT460", "TT280", "TT420", "TT460",
    "Capoforte CX240", "Capoforte SX200", "Capoforte FX240", "Capoforte SX240"])
V("Karnic", "MOTORYACHT", "https://www.karnic.eu/", [
    "SL600", "SL602", "SL652", "SL702", "SL800", "SL852", "Bluewater 2251", "Bluewater 2452",
    "SL702 Flexi", "SL903"])
V("Parker Poland", "MOTORYACHT", "https://www.parkerpoland.com/", [
    ("Parker 660", 6.6), ("Parker 750", 7.5), ("Parker 850", 8.5), "Parker 630", "Parker 760",
    "Parker 800", "Parker 920", "Parker 110"])

# ── RIBs ──
V("Zodiac", "RIB", "https://www.zodiac-nautic.com/", L("Medline ", [5.5, 6.8, 7.5, 9])
  + L("Pro ", [5.5, 7, 9]) + L("Open ", [4.2, 5.5, 6.5, 7]) + L("N-ZO ", [600, 700, 760])
  + ["Cadet 340", "Cadet 390", "eOpen 4.2"])
V("Grand Marine", "RIB", "https://www.grandmarine.com/", L("Golden Line G", [420, 500, 580, 650, 750, 850])
  + L("Silver Line S", [300, 330, 370, 420, 470]) + ["Drive D600", "Drive D700"])
V("BRIG", "RIB", "https://www.brig-boats.com/", L("Eagle ", [300, 340, 380, 450, 500, 650, 670, 780])
  + L("Navigator ", [485, 520, 570, 610, 700]) + L("Falcon Tender ", [300, 330, 360]))
V("Highfield Boats", "RIB", "https://www.highfieldboats.com/", L("Sport ", [300, 330, 390, 460, 560, 660])
  + L("Ocean ", [400, 460, 500, 540, 590]) + L("Classic ", [290, 310, 360, 380]))
V("Nuova Jolly", "RIB", "https://www.nuovajollymarine.com/", [
    "Prince 21", "Prince 25", "Prince 28", "Prince 33", "Prince 35", "Prince 38 CC",
    "NJ 700 XL", "NJ 800 XL", "NJ 900 XL", "NJ 1000 XL"])

# ── PWC ──
V("Sea-Doo", "PWC", "https://www.sea-doo.com/", [
    ("Spark", None, 2014), "Spark Trixx", "GTI 90", "GTI 130", "GTI SE 170", "GTR 230",
    "GTX 170", "GTX 230", "GTX Limited 300", "RXP-X 300", "RXT-X 300", "Wake 170", "Wake Pro 230",
    ("Fish Pro Trophy", None, 2019), "Fish Pro Sport", "Explorer Pro 170", "SwitchCruise"])
V("Yamaha WaveRunner", "PWC", "https://www.yamahawaverunners.com/", [
    "EX", "EX Sport", "EX Deluxe", "EX Limited", "VX", "VX Cruiser", "VX Deluxe", "VX Limited",
    "FX HO", "FX Cruiser HO", "FX SVHO", "FX Cruiser SVHO", "GP1800R HO", "GP1800R SVHO",
    "SuperJet", "JetBlaster"])
V("Kawasaki Jet Ski", "PWC", "https://www.kawasaki.com/", [
    "STX 160", "STX 160X", "STX 160LX", "Ultra 160LX", "Ultra 310X", "Ultra 310LX",
    "Ultra 310R", "SX-R 160", "Ultra 160LX-S", "Ultra 310LX-S"], review=True)


# ───────────────────────────── ENGINE BRANDS ─────────────────────────────

ENGINE_BRANDS = [
    # outboards
    ("Yamaha",         "https://www.yamahaoutboards.com/",        False),
    ("Mercury Marine", "https://www.mercurymarine.com/",          False),
    ("Suzuki Marine",  "https://www.suzukimarine.com/",           False),
    ("Honda Marine",   "https://marine.honda.com/",               False),
    ("Tohatsu",        "https://www.tohatsu.com/",                False),
    ("Parsun",         "https://www.parsun.com/",                 True),
    ("Selva Marine",   "https://www.selvamarine.com/",            True),
    ("Evinrude",       "https://www.evinrude.com/",               True),
    # electric outboards
    ("Torqeedo",       "https://www.torqeedo.com/",               True),
    ("ePropulsion",    "https://www.epropulsion.com/",            True),
    # inboard / diesel
    ("Volvo Penta",    "https://www.volvopenta.com/",             False),
    ("Yanmar",         "https://www.yanmar.com/marine/",          False),
    ("Cummins",        "https://www.cummins.com/engines/marine",  False),
    ("Caterpillar",    "https://www.cat.com/en_US/products/new/power-systems/marine-power-systems.html", False),
    ("MAN Engines",    "https://www.man-engines.com/",            False),
    ("MTU",            "https://www.mtu-solutions.com/",          True),
    ("Scania",         "https://www.scania.com/products-and-services/engines/marine/", True),
    ("John Deere",     "https://www.deere.com/en/marine-engines/", True),
    ("FPT Industrial", "https://www.fptindustrial.com/",          True),
    ("Nanni Diesel",   "https://www.nannienergy.com/",            True),
    ("Vetus",          "https://www.vetus.com/",                  True),
    ("Steyr Motors",   "https://www.steyr-motors.com/",           True),
    ("Beta Marine",    "https://www.betamarine.co.uk/",           True),
]


# ───────────────────────────── ENGINE MODELS ─────────────────────────────

# ── Outboards (name encodes HP) ──
E("Yamaha", "GASOLINE", "OUTBOARD", "https://www.yamahaoutboards.com/", "F",
  [2.5, 4, 6, 8, 9.9, 15, 20, 25, 30, 40, 50, 60, 70, 90, 115, 130, 150, 175, 200, 225, 250, 300])
E("Yamaha", "GASOLINE", "OUTBOARD", "https://www.yamahaoutboards.com/", "XTO ", [425, 450])

E("Mercury Marine", "GASOLINE", "OUTBOARD", "https://www.mercurymarine.com/", "FourStroke ",
  [2.5, 3.5, 4, 5, 6, 8, 9.9, 15, 20, 25, 30, 40, 50, 60, 75, 90, 100, 115, 150, 175, 200, 225, 250, 300], sep="")
E("Mercury Marine", "GASOLINE", "OUTBOARD", "https://www.mercurymarine.com/", "Verado ", [250, 300, 350, 400])
E("Mercury Marine", "GASOLINE", "OUTBOARD", "https://www.mercurymarine.com/", "Pro XS ", [115, 150, 175, 200, 225, 250, 300])
E("Mercury Marine", "GASOLINE", "OUTBOARD", "https://www.mercurymarine.com/", "", [450, 500], sep="", )
EL("Mercury Marine", "GASOLINE", "STERNDRIVE", "https://www.mercurymarine.com/",
   [("MerCruiser 4.5L 250", 250), ("MerCruiser 6.2L 300", 300), ("MerCruiser 6.2L 350", 350),
    ("MerCruiser 8.2L 380", 380), ("MerCruiser 8.2L 430", 430), ("MerCruiser Bravo 4.5 250", 250)])

E("Suzuki Marine", "GASOLINE", "OUTBOARD", "https://www.suzukimarine.com/", "DF",
  [2.5, 4, 6, 9.9, 15, 20, 25, 30, 40, 50, 60, 70, 90, 100, 115, 140, 150, 175, 200, 250, 300, 350])

E("Honda Marine", "GASOLINE", "OUTBOARD", "https://marine.honda.com/", "BF",
  [2.3, 4, 5, 6, 8, 9.9, 15, 20, 25, 30, 40, 50, 60, 75, 90, 100, 115, 135, 150, 175, 200, 225, 250])

E("Tohatsu", "GASOLINE", "OUTBOARD", "https://www.tohatsu.com/", "MFS",
  [2.5, 3.5, 4, 5, 6, 8, 9.8, 9.9, 15, 20, 25, 30, 40, 50, 60, 75, 90, 100, 115, 140])

E("Parsun", "GASOLINE", "OUTBOARD", "https://www.parsun.com/", "F",
  [2.6, 4, 5, 6, 8, 9.8, 15, 20, 25, 40, 50, 60])

E("Selva Marine", "GASOLINE", "OUTBOARD", "https://www.selvamarine.com/", "Selva ",
  [2.5, 5, 6, 9.9, 15, 25, 30, 40, 60, 90, 115, 150, 200, 250, 300])

EL("Evinrude", "GASOLINE", "OUTBOARD", "https://www.evinrude.com/",
   [("E-TEC G2 115", 115), ("E-TEC G2 140", 140), ("E-TEC G2 150", 150), ("E-TEC G2 175", 175),
    ("E-TEC G2 200", 200), ("E-TEC G2 225", 225), ("E-TEC G2 250", 250), ("E-TEC G2 300", 300),
    ("E-TEC 90", 90), ("E-TEC 115", 115), ("E-TEC 150", 150), ("E-TEC 200", 200)])

# ── Electric outboards (hp = manufacturer-stated equivalent) ──
EL("Torqeedo", "ELECTRIC", "OUTBOARD", "https://www.torqeedo.com/",
   [("Travel 1103", 3), ("Travel 1103 C", 3), ("Cruise 3.0", 8), ("Cruise 6.0", 15),
    ("Cruise 12.0", 25), ("Deep Blue 50", 50), ("Deep Blue 100", 100)])
EL("ePropulsion", "ELECTRIC", "OUTBOARD", "https://www.epropulsion.com/",
   [("Spirit 1.0 Plus", 3), ("Navy 3.0", 8), ("Navy 6.0", 15), ("X12", 25), ("X20", 40)])

# ── Inboard diesels (rating suffix = HP unless noted) ──
_vp = "https://www.volvopenta.com/"
E("Volvo Penta", "DIESEL", "INBOARD", _vp, "D1-", [13, 20, 30])
E("Volvo Penta", "DIESEL", "INBOARD", _vp, "D2-", [40, 50, 60, 75])
E("Volvo Penta", "DIESEL", "INBOARD", _vp, "D3-", [110, 140, 170, 200, 220])
E("Volvo Penta", "DIESEL", "INBOARD", _vp, "D4-", [175, 230, 270, 300, 320])
E("Volvo Penta", "DIESEL", "INBOARD", _vp, "D6-", [280, 330, 380, 440, 480])
E("Volvo Penta", "DIESEL", "INBOARD", _vp, "D8-", [510, 550, 600])
E("Volvo Penta", "DIESEL", "INBOARD", _vp, "D11-", [510, 625, 670, 725])
E("Volvo Penta", "DIESEL", "INBOARD", _vp, "D13-", [800, 900, 1000])
E("Volvo Penta", "GASOLINE", "STERNDRIVE", _vp, "V6-", [200, 240, 280])
E("Volvo Penta", "GASOLINE", "STERNDRIVE", _vp, "V8-", [350, 380, 430])
EL("Volvo Penta", "DIESEL", "INBOARD", _vp,
   [("IPS 600", 435), ("IPS 650", 480), ("IPS 700", 520), ("IPS 800", 600),
    ("IPS 900", 700), ("IPS 950", 725), ("IPS 1050", 800), ("IPS 1350", 1000)], review=True)

EL("Yanmar", "DIESEL", "INBOARD", "https://www.yanmar.com/marine/",
   [("1GM10", 9), ("2YM15", 14), ("3YM20", 21), ("3YM30", 29), ("3JH40", 40), ("4JH45", 45),
    ("4JH57", 57), ("4JH80", 80), ("4JH110", 110), ("4LV150", 150), ("4LV170", 170),
    ("4LV195", 195), ("4LV230", 230), ("4LV250", 250), ("6LY400", 400), ("6LY440", 440),
    ("6LF530", 530), ("6LF550", 550), ("8LV320", 320), ("8LV350", 350), ("8LV370", 370)])

_cum = "https://www.cummins.com/engines/marine"
E("Cummins", "DIESEL", "INBOARD", _cum, "QSB6.7 ", [230, 305, 355, 380, 425, 480, 500, 550], sep="")
E("Cummins", "DIESEL", "INBOARD", _cum, "QSC8.3 ", [500, 550, 600], sep="")
E("Cummins", "DIESEL", "INBOARD", _cum, "QSL9 ", [405, 503, 600, 715], sep="")
E("Cummins", "DIESEL", "INBOARD", _cum, "QSM11 ", [610, 670, 715], sep="")

_cat = "https://www.cat.com/"
E("Caterpillar", "DIESEL", "INBOARD", _cat, "C7.1 ", [460, 510, 650], sep="")
E("Caterpillar", "DIESEL", "INBOARD", _cat, "C8.7 ", [600, 650, 700, 800], sep="")
E("Caterpillar", "DIESEL", "INBOARD", _cat, "C12.9 ", [800, 850, 1000], sep="")
E("Caterpillar", "DIESEL", "INBOARD", _cat, "C18 ", [1001, 1136, 1150], sep="")
E("Caterpillar", "DIESEL", "INBOARD", _cat, "C32 ", [1600, 1800, 1900], sep="")

_man = "https://www.man-engines.com/"
E("MAN Engines", "DIESEL", "INBOARD", _man, "i6-", [730, 800])
E("MAN Engines", "DIESEL", "INBOARD", _man, "V8-", [1000, 1200])
E("MAN Engines", "DIESEL", "INBOARD", _man, "V12-", [1400, 1550, 1650, 1800, 1900, 2000])

EL("MTU", "DIESEL", "INBOARD", "https://www.mtu-solutions.com/",
   [("8V 2000 M86", 1200), ("10V 2000 M86", 1500), ("12V 2000 M96", 1630),
    ("16V 2000 M96", 2000), ("10V 2000 M72", 1440), ("8V 2000 M72", 1152)])

EL("Scania", "DIESEL", "INBOARD", "https://www.scania.com/products-and-services/engines/marine/",
   [("DI13 070M", 550), ("DI13 080M", 650), ("DI13 087M", 700), ("DI16 070M", 700),
    ("DI16 083M", 900), ("DI16 090M", 1000), ("DI16 095M", 1150)])

EL("John Deere", "DIESEL", "INBOARD", "https://www.deere.com/en/marine-engines/",
   [("PowerTech 4045 99", 99), ("PowerTech 4045 150", 150), ("PowerTech 6068 250", 250),
    ("PowerTech 6068 300", 300), ("PowerTech 6090 375", 375), ("PowerTech 6090 425", 425)])

EL("FPT Industrial", "DIESEL", "INBOARD", "https://www.fptindustrial.com/",
   [("N40 250", 250), ("N60 370", 370), ("N67 570", 570), ("C90 650", 650),
    ("Cursor 9 570", 570), ("Cursor 13 800", 800), ("Cursor 16 1000", 1000)])

EL("Nanni Diesel", "DIESEL", "INBOARD", "https://www.nannienergy.com/",
   [("N2.10", 10), ("N3.21", 21), ("N3.30", 29), ("N4.38", 37), ("N4.50", 50), ("N4.60", 60),
    ("N4.65", 65), ("N4.80", 80), ("N4.100", 100), ("N4.115", 115), ("N5.250", 250),
    ("N6.280", 280), ("N6.380", 380)])

EL("Vetus", "DIESEL", "INBOARD", "https://www.vetus.com/",
   [("M2.06", 11), ("M2.18", 16), ("M3.29", 29), ("M4.45", 42), ("M4.56", 53), ("M4.65", 60),
    ("VF4.140", 140), ("VF5.220", 220), ("DEUTZ DT44", 140), ("DEUTZ DT66", 170)])

EL("Steyr Motors", "DIESEL", "INBOARD", "https://www.steyr-motors.com/",
   [("SE126", 126), ("SE156", 156), ("SE196", 196), ("SE236", 236), ("SE266", 266),
    ("SE286", 286), ("SE306", 306)])

E("Beta Marine", "DIESEL", "INBOARD", "https://www.betamarine.co.uk/", "Beta ",
  [14, 16, 20, 25, 30, 35, 38, 43, 50, 60, 75, 85, 90, 105, 115, 150], sep="")


# ───────────────── SUPPLEMENTAL v1.1 — archived / additional real models ─────────────────
# Back-catalog + further current models for brands with large ranges. Year left null
# (→ needsReview) unless confidently known. Dedupe (brand+model) drops any overlap.

# Beneteau back-catalog
V("Beneteau", "SAILBOAT", _bene, ["Sense 43", "Sense 46", "Sense 50", "Sense 51", "Sense 55", "Sense 57"]
  + L("Oceanis ", [331, 343, 352, 361, 373, 381, 393, 411, 423, 461, 473])
  + ["First 20", "First 25S", "First 30", "First 35", "First 40", "First 45", "First 47.7", "First 50", "Figaro 3"])
V("Beneteau", "MOTORYACHT", _bene, ["Antares 6", "Antares 30S", "Antares 36", "Gran Turismo 38", "Gran Turismo 44",
                                    "Gran Turismo 49", "Monte Carlo 5", "Monte Carlo 6", "Flyer 5.5", "Flyer 6.6"])
# Jeanneau back-catalog
V("Jeanneau", "SAILBOAT", _jean, L("Sun Odyssey ", [30, 32, 33, 36, 37, 40, 42, 43, 45, 49, 50])
  + ["Sun Odyssey 36i", "Sun Odyssey 42i", "Sun Odyssey 45 DS", "Sun Odyssey 50 DS", "Fantasia 27", "Espace 990"])
V("Jeanneau", "MOTORYACHT", _jean, ["Merry Fisher 585", "Merry Fisher 625", "Merry Fisher 755", "Merry Fisher 855",
                                    "Merry Fisher 1025", "Cap Camarat 4.5", "Cap Camarat 5.1", "Cap Camarat 6.0",
                                    "Cap Camarat 8.0", "Cap Camarat 10.5 WA", "Prestige 34", "NC 33", "NC 14"])
# Bavaria back-catalog
V("Bavaria Yachts", "SAILBOAT", _bav, L("Bavaria ", [30, 31, 33, 34, 35, 37, 38, 39, 40, 42, 44, 46, 47, 49, 50], sep="")
  + ["Vision 42", "Vision 46", "Cruiser 33", "Cruiser 36", "Cruiser 45", "Cruiser 50", "Cruiser 56"])
# Sea Ray back-catalog
V("Sea Ray", "MOTORYACHT", _sr, L("Sundancer ", [245, 260, 280, 300, 340, 380, 400, 450, 510, 540, 580, 610])
  + ["SLX 210", "SLX 250", "L550", "L590", "L650", "Fly 400", "Fly 460", "310 Sundancer", "350 Sundancer"])
# Bayliner back-catalog
V("Bayliner", "MOTORYACHT", _bay, L("Ciera ", [1850, 1950, 2050, 2150, 2450, 2655, 2855, 3055])
  + ["Capri 1750", "Rendezvous 2109", "Discovery 246", "Discovery 266"])
# Boston Whaler back-catalog
V("Boston Whaler", "MOTORYACHT", _bw, ["130 Super Sport", "150 Super Sport", "170 Montauk", "240 Dauntless",
                                       "270 Dauntless", "285 Conquest", "315 Conquest", "345 Conquest",
                                       "350 Realm", "405 Conquest", "420 Outrage", "160 Super Sport", "230 Vantage"])
# Princess back-catalog
V("Princess Yachts", "MOTORYACHT", _pri, L("Princess ", [42, 45, 49, 50, 54, 56, 58, 60, 62, 64, 67, 72, 75, 82, 88, 95, 98])
  + ["V39", "V42", "V48", "V52", "V58", "V72"])
# Sunseeker back-catalog
V("Sunseeker", "MOTORYACHT", _sun, ["Predator 68", "Predator 72", "Predator 80", "Predator 108", "Manhattan 52",
                                    "Manhattan 62", "Manhattan 70", "Manhattan 73", "Portofino 35", "Portofino 48",
                                    "Sport Yacht 74", "80 Sport Yacht", "131 Yacht", "155 Yacht", "116 Yacht"])
# Azimut back-catalog
V("Azimut Yachts", "MOTORYACHT", _azi, L("Flybridge ", [40, 43, 45, 50, 55, 60, 64, 66, 84, 88])
  + ["Atlantis 34", "Atlantis 38", "Atlantis 43", "Atlantis 50", "Magellano 53", "Magellano 76", "S5", "Verve 42", "Verve 47"])
# Fairline back-catalog
V("Fairline", "MOTORYACHT", _fai, ["Targa 34", "Targa 37", "Targa 47", "Targa 48", "Targa 52", "Targa 62",
                                   "Squadron 42", "Squadron 48", "Squadron 55", "Squadron 65", "Squadron 78",
                                   "Phantom 40", "Phantom 43", "Phantom 48"])
# Galeon extra
V("Galeon", "MOTORYACHT", _gal, ["Galeon 265", "Galeon 305", "Galeon 385", "Galeon 470", "Galeon 550 Fly",
                                 "Galeon 640 Fly", "Galeon 430 Skydeck", "Galeon 470 Skydeck"])
# Cranchi extra
V("Cranchi", "MOTORYACHT", _cra, ["Cranchi 50 Mediterranee", "Cranchi 60 HT", "Endurance 30", "Endurance 33",
                                  "Endurance 39", "Endurance 45", "Cranchi 56 HT", "Cranchi E40"])
# Riva extra
V("Riva", "MOTORYACHT", "https://www.riva-yacht.com/", ["Riva 63 Vertigo", "Riva 66 Ribelle", "Riva 88 Miami",
                                                        "Rivale 56", "Sportriva 56", "Riva 82 Diva"])
# Nimbus / Sealine / Prestige extra
V("Nimbus", "MOTORYACHT", "https://www.nimbus.se/", ["Nimbus 21", "Nimbus 305 Drophead", "Nimbus 335 Coupé",
                                                     "Nimbus 335 Sport", "Nimbus 250 Coupé"])
V("Sealine", "MOTORYACHT", _sea, ["Sealine S24", "Sealine C48", "Sealine F42", "Sealine T50", "Sealine SC29"])
V("Prestige Yachts", "MOTORYACHT", _pre, ["Prestige 550", "Prestige 620", "Prestige 680", "Prestige M6", "Prestige M7"])
# Absolute / Sanlorenzo extra
V("Absolute Yachts", "MOTORYACHT", _abs, ["Absolute 50 Fly", "Absolute 62 Fly", "Absolute 47 Fly", "Absolute 72 Fly"])
V("Sanlorenzo", "SUPERYACHT", "https://www.sanlorenzoyacht.com/", ["SL78 Asymmetric", "SX112", "SD126", "SP92"])

# ── Additional engine brands + ratings ──
ENGINE_BRANDS += [
    ("Doosan",    "https://www.doosanengine.com/",  True),
    ("Baudouin",  "https://www.baudouin.com/",      True),
]
EL("Doosan", "DIESEL", "INBOARD", "https://www.doosanengine.com/",
   [("L086 300", 300), ("V158 700", 700), ("V180 820", 820), ("V222 1000", 1000),
    ("V260 1200", 1200), ("4V222 1440", 1440)])
EL("Baudouin", "DIESEL", "INBOARD", "https://www.baudouin.com/",
   [("4W105 150", 150), ("6W105 250", 250), ("6M21 400", 400), ("6M26 600", 600),
    ("8M26 800", 800), ("12M26 1200", 1200)])
# Mercury Diesel (TDI-based) under Mercury brand
EL("Mercury Marine", "DIESEL", "INBOARD", "https://www.mercurymarine.com/",
   [("Mercury Diesel 3.0L 230", 230), ("Mercury Diesel 4.2L 335", 335), ("Mercury Diesel 4.2L 370", 370),
    ("Mercury Diesel 6.7L 480", 480), ("Mercury Diesel 6.7L 550", 550)])
# Volvo Penta D16 big-block diesels
E("Volvo Penta", "DIESEL", "INBOARD", _vp, "D16-", [550, 650, 750, 900, 1000])
# Yanmar big commercial + Cummins X-series + Scania/JD extra
EL("Yanmar", "DIESEL", "INBOARD", "https://www.yanmar.com/marine/",
   [("6AYM-WGT 900", 900), ("6AYM-WET 1080", 1080), ("6AYM-WST 1200", 1200), ("6HYM-WET 480", 480)])
EL("Cummins", "DIESEL", "INBOARD", _cum,
   [("X12 600", 600), ("X15 650", 650), ("X15 750", 750), ("X15 800", 800)])
EL("Scania", "DIESEL", "INBOARD", "https://www.scania.com/products-and-services/engines/marine/",
   [("DI09 350M", 350), ("DI09 425M", 425), ("DI13 065M", 500)])
EL("John Deere", "DIESEL", "INBOARD", "https://www.deere.com/en/marine-engines/",
   [("PowerTech 4045 200", 200), ("PowerTech 6068 350", 350)])
