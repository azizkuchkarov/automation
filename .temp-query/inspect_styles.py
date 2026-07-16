import sys, zipfile, re
from xml.etree import ElementTree as ET

path = sys.argv[1]
out = sys.argv[2]
with zipfile.ZipFile(path) as z:
    sheet = z.read("xl/worksheets/sheet1.xml").decode("utf-8")
    styles = z.read("xl/styles.xml").decode("utf-8")

lines = []
m = re.search(r'<row r="4"([^>]*)>', sheet)
lines.append(f"row4 attrs: {m.group(1) if m else 'none'}")

root = ET.fromstring(styles)
ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
for xf in root.findall(f".//{{{ns}}}cellXfs/{{{ns}}}xf"):
    idx = root.findall(f".//{{{ns}}}cellXfs/{{{ns}}}xf").index(xf)
    if idx in (44, 48, 49):
        lines.append(f"style {idx}: {ET.tostring(xf, encoding='unicode')[:300]}")

open(out, "w", encoding="utf-8").write("\n".join(lines))
