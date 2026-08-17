#!/usr/bin/env python3
# DEV-ONLY asset generator — DO NOT COMMIT. Produces small, REAL, visually-distinct PNGs + PDFs
# (pure stdlib: no PIL/ImageMagick) so the admin media/evidence seed wires objectful files that
# actually render. Idempotent: writes into the given dir (default: ./assets). Re-runnable.
import os, sys, zlib, struct

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "assets")
os.makedirs(OUT, exist_ok=True)

def _png(path, w, h, painter):
    """painter(x,y)->(r,g,b). Writes a truecolor PNG."""
    raw = bytearray()
    for y in range(h):
        raw.append(0)  # filter type 0 (None) per scanline
        for x in range(w):
            r, g, b = painter(x, y)
            raw += bytes((r & 255, g & 255, b & 255))
    def chunk(typ, data):
        c = typ + data
        return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c) & 0xffffffff)
    sig = b"\x89PNG\r\n\x1a\n"
    ihdr = struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)  # 8-bit truecolor RGB
    idat = zlib.compress(bytes(raw), 9)
    with open(path, "wb") as f:
        f.write(sig + chunk(b"IHDR", ihdr) + chunk(b"IDAT", idat) + chunk(b"IEND", b""))
    print(f"  png  {os.path.basename(path)} ({w}x{h}, {os.path.getsize(path)}B)")

def grad(top, bottom, border, band=None):
    """Vertical gradient top->bottom, colored border, optional diagonal accent band."""
    tr, tg, tb = top; br_, bg, bb = bottom; er, eg, eb = border
    def paint(x, y, w=640, h=480):
        if x < 12 or x > w - 13 or y < 12 or y > h - 13:
            return (er, eg, eb)
        if band and abs((x % 160) - (y % 160)) < 26:      # diagonal accent stripes
            return band
        t = y / (h - 1)
        return (int(tr + (br_ - tr) * t), int(tg + (bg - tg) * t), int(tb + (bb - tb) * t))
    return paint

W, H = 640, 480
SPECS = {
    # vessel surfaces
    "vessel_cover.png":     grad((28, 78, 140), (10, 30, 66),  (240, 245, 255), band=(255, 214, 90)),   # navy hero + gold
    "vessel_second.png":    grad((14, 108, 120), (6, 42, 52),  (220, 245, 245), band=(255, 255, 255)),   # teal
    # SR surfaces
    "sr_request.png":       grad((150, 60, 40), (70, 22, 16),  (255, 236, 224), band=(255, 180, 120)),   # rust (owner request)
    "sr_completion.png":    grad((34, 120, 58), (14, 54, 26),  (232, 255, 236), band=(180, 255, 190)),   # green (provider proof)
    "sr_worklog.png":       grad((90, 70, 150), (38, 28, 74),  (238, 232, 255), band=(200, 180, 255)),   # violet (work log)
}
for name, painter in SPECS.items():
    p = os.path.join(OUT, name)
    if not os.path.exists(p):
        _png(p, W, H, lambda x, y, _p=painter: _p(x, y))
    else:
        print(f"  keep {name}")

def _pdf(path, title, lines):
    """Minimal valid single-page PDF with visible Helvetica text."""
    body = f"BT /F1 22 Tf 60 720 Td ({title}) Tj ET\n"
    yy = 680
    for ln in lines:
        body += f"BT /F1 13 Tf 60 {yy} Td ({ln}) Tj ET\n"
        yy -= 22
    objs = [
        "<< /Type /Catalog /Pages 2 0 R >>",
        "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
        f"<< /Length {len(body)} >>\nstream\n{body}\nendstream",
        "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
    ]
    out = "%PDF-1.4\n"; offsets = []
    for i, o in enumerate(objs, 1):
        offsets.append(len(out)); out += f"{i} 0 obj\n{o}\nendobj\n"
    xref = len(out)
    out += f"xref\n0 {len(objs)+1}\n0000000000 65535 f \n"
    for off in offsets:
        out += f"{off:010d} 00000 n \n"
    out += f"trailer\n<< /Size {len(objs)+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"
    with open(path, "wb") as f:
        f.write(out.encode("latin-1"))
    print(f"  pdf  {os.path.basename(path)} ({os.path.getsize(path)}B)")

for name, title, lines in [
    ("vessel_doc.pdf", "[DEV-SEED] Vessel Registration Certificate",
     ["Vessel ID: 100013", "Document type: REGISTRATION", "Issued: dev-seed (not a real certificate)."]),
    ("sr_request.pdf", "[DEV-SEED] Service Request Document",
     ["Service Request: 30001", "Attachment type: request document", "Uploaded by owner (dev-seed)."]),
]:
    p = os.path.join(OUT, name)
    _pdf(p, title, lines) if not os.path.exists(p) else print(f"  keep {name}")

print(f"assets ready in {OUT}")
