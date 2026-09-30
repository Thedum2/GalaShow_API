"""Build dependency-free SVG/PDF fixtures for the development database seed."""
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[3]
OUTPUT = ROOT / "Client/public/sample-data/v1"


def write_pdf(path, title):
    lines = ["GALASHOW - SAMPLE DOCUMENT", title,
             "For development UI verification only.",
             "This is not an actual legal policy or agreement.",
             "Replace this sample before publishing real service policies."]
    content = "BT /F1 18 Tf 50 780 Td "
    content += " 0 -32 Td ".join(f"({line}) Tj" for line in lines) + " ET"
    stream = content.encode("ascii")
    objects = [b"<< /Type /Catalog /Pages 2 0 R >>",
               b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
               b"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 840 900] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
               b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
               f"<< /Length {len(stream)} >>\nstream\n".encode() + stream + b"\nendstream"]
    data = bytearray(b"%PDF-1.4\n")
    offsets = [0]
    for number, obj in enumerate(objects, 1):
        offsets.append(len(data))
        data.extend(f"{number} 0 obj\n".encode() + obj + b"\nendobj\n")
    xref = len(data)
    data.extend(f"xref\n0 {len(offsets)}\n0000000000 65535 f \n".encode())
    for offset in offsets[1:]:
        data.extend(f"{offset:010d} 00000 n \n".encode())
    data.extend(f"trailer\n<< /Size {len(offsets)} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n".encode())
    path.write_bytes(data)


def build():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    colors = {"purple": "#7c3aed", "blue": "#2563eb", "green": "#059669", "pink": "#db2777"}
    game_colors = {**colors, "orange": "#ea580c"}
    for name, color in colors.items():
        avatar = f'''<svg xmlns="http://www.w3.org/2000/svg" width="160" height="160" viewBox="0 0 160 160">
<title>GalaShow sample avatar: {name}</title>
<rect width="160" height="160" rx="36" fill="{color}"/>
<circle cx="58" cy="66" r="9" fill="white"/><circle cx="102" cy="66" r="9" fill="white"/>
<path d="M48 99 Q80 126 112 99" fill="none" stroke="white" stroke-width="8" stroke-linecap="round"/>
</svg>'''
        (OUTPUT / f"avatar-{name}.svg").write_text(avatar, encoding="utf-8")
        if name != "pink":
            background = f'''<svg xmlns="http://www.w3.org/2000/svg" width="1920" height="1080" viewBox="0 0 1920 1080">
<title>GalaShow sample background: {name}</title>
<defs><linearGradient id="g" x2="1" y2="1"><stop stop-color="#111827"/><stop offset="1" stop-color="{color}"/></linearGradient></defs>
<rect width="1920" height="1080" fill="url(#g)"/>
<circle cx="1600" cy="150" r="380" fill="white" opacity=".06"/>
<circle cx="200" cy="1050" r="500" fill="white" opacity=".04"/>
<text x="80" y="990" fill="white" opacity=".6" font-family="sans-serif" font-size="30">GALASHOW / DEVELOPMENT SAMPLE</text>
</svg>'''
            (OUTPUT / f"background-{name}.svg").write_text(background, encoding="utf-8")
    for kind, label, color in [("choice", "LEFT / RIGHT", game_colors["purple"]),
                               ("vote", "1 / 2 / 3", game_colors["blue"]),
                               ("survival", "SURVIVAL", game_colors["green"]),
                               ("minority", "A / B MINORITY", game_colors["orange"])]:
        logo = f'''<svg xmlns="http://www.w3.org/2000/svg" width="480" height="320" viewBox="0 0 480 320">
<title>GalaShow sample game: {kind}</title><rect width="480" height="320" rx="32" fill="{color}"/>
<text x="240" y="160" text-anchor="middle" fill="white" font-family="sans-serif" font-size="42" font-weight="bold">{label}</text>
<text x="240" y="230" text-anchor="middle" fill="white" font-family="sans-serif" font-size="18">GALASHOW / SAMPLE</text></svg>'''
        (OUTPUT / f"game-{kind}.svg").write_text(logo, encoding="utf-8")
    for icon in ["youtube", "chzzk", "soop"]:
        shutil.copyfile(ROOT / f"Client/src/assets/svg/{icon}.svg", OUTPUT / f"{icon}.svg")
    write_pdf(OUTPUT / "sample-terms.pdf", "Sample Terms of Service")
    write_pdf(OUTPUT / "sample-privacy.pdf", "Sample Privacy Policy")
    print(f"Built 16 development sample assets in {OUTPUT}")


if __name__ == "__main__":
    build()
