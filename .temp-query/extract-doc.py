import pathlib
import re

for label, name in [("material", "RfqMaterial.doc"), ("service", "RfqService.doc")]:
    p = pathlib.Path(r"C:\Users\a.kuchkarov\Desktop\automation\ATG.Platform.Infrastructure\Dcs\Templates") / name
    data = p.read_bytes()
    text = data.decode("cp1251", errors="ignore")
    print("===", label, "===")
    for line in text.split("\n"):
        if any(k in line for k in ["Тема", "просит", "Контакт", "ATG", "электрон", "Ф.И.О", "Запрос", "срок"]):
            print(repr(line.strip()[:200]))
