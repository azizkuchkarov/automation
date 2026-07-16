import re
import sys
import zipfile

sys.stdout.reconfigure(encoding="utf-8")
path = r"C:\Users\a.kuchkarov\Desktop\automation\ATG.Platform.Infrastructure\Dcs\Templates\RfqMaterial.docx"
with zipfile.ZipFile(path) as z:
    xml = z.read("word/document.xml").decode()
paras = re.findall(r"<w:p[ >].*?</w:p>", xml, re.S)
for i, p in enumerate(paras):
    text = "".join(re.findall(r"<w:t[^>]*>([^<]*)</w:t>", p))
    if i in [2, 12, 14, 20, 23, 31, 40, 48, 50, 51, 52, 58, 59, 60] or "later than" in text or "provide your" in text:
        szs = re.findall(r'<w:sz w:val="(\d+)"', p)
        print(i, repr(text[:180]))
        print("  sz", szs[:8])
