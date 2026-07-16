import sys, zipfile, re
path = sys.argv[1]
out = sys.argv[2]
with zipfile.ZipFile(path) as z:
    xml = z.read("xl/worksheets/sheet1.xml").decode("utf-8")
m = re.search(r'<row r="4"[^>]*>.*?</row>', xml, re.DOTALL)
open(out, "w", encoding="utf-8").write(m.group(0) if m else "not found")
