import sys
import zipfile
import re
from pathlib import Path

path = Path(sys.argv[1])
out = Path(sys.argv[2]) if len(sys.argv) > 2 else Path("inspect-out.txt")
with zipfile.ZipFile(path) as z:
    xml = z.read("xl/worksheets/sheet1.xml").decode("utf-8")

lines = []
for row in (2, 4, 7, 8, 10, 37):
    m = re.search(rf'<row r="{row}"[^>]*>(.*?)</row>', xml, re.DOTALL)
    lines.append(f"ROW {row}: {m.group(1)[:500] if m else 'NOT FOUND'}")

out.write_text("\n".join(lines), encoding="utf-8")
print("wrote", out)
