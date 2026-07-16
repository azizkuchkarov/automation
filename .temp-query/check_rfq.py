import re
import sys
import zipfile

sys.stdout.reconfigure(encoding="utf-8")
path = r"C:\Users\a.kuchkarov\Desktop\automation\.temp-query\RfqGenTest\.temp-query\rfq-test-output.docx"
with zipfile.ZipFile(path) as z:
    xml = z.read("word/document.xml").decode()
text = re.sub(r"<[^>]+>", "", xml)
for key in [
    "no later than 15.07.2026",
    "a.madrakhimov@atg.uz",
    "j.zokirov@atg.uz",
    "engineer@atg.uz",
]:
    print(key, key in text)

runs = re.findall(r"<w:r[^>]*>.*?</w:r>", xml, re.S)
for r in runs:
    t = "".join(re.findall(r"<w:t[^>]*>([^<]*)</w:t>", r))
    if "ATG-CP-MT-LO-2026" in t or "Subject:" in t:
        sz = re.search(r'<w:sz w:val="(\d+)"', r)
        print("RUN", repr(t[:60]), "sz", sz.group(1) if sz else None)
