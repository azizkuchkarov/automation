import re
import sys

path = sys.argv[1]
with open(path, "rb") as f:
    data = f.read()

raw = data.decode("latin1", errors="ignore")
runs = re.findall(r"[\u0400-\u04ffA-Za-z0-9\s\.,\-\(\):;\"'\/№]{6,}", raw)
seen = set()
for r in runs:
    r = " ".join(r.split())
    if r in seen or "Microsoft" in r or "Times New Roman" in r:
        continue
    if sum(c.isalpha() for c in r) < 4:
        continue
    seen.add(r)
    print(r)
