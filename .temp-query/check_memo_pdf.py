import fitz

doc = fitz.open(r"C:\Users\a.kuchkarov\Desktop\automation\.temp-query\memo-template-test.pdf")
t = doc[0].get_text()
out = r"C:\Users\a.kuchkarov\Desktop\automation\.temp-query\memo-font-check.txt"
with open(out, "w", encoding="utf-8") as f:
    f.write(f"cyrillic_chars={sum(1 for ch in t if '\u0400' <= ch <= '\u04FF')}\n")
    f.write(f"has_kuchkarov_ru={'Кучкаров' in t}\n")
    f.write(f"has_proverka={'Проверка' in t}\n")
    f.write(f"has_sluzhebnaya={'СЛУЖЕБНАЯ' in t}\n")
    f.write(f"has_question_marks={'????' in t}\n")
print("ok")
