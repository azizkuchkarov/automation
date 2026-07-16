# -*- coding: utf-8 -*-
from pathlib import Path
import win32com.client

out_dir = Path(r"C:\Users\a.kuchkarov\Desktop\automation\.temp-query\prikaz-template")
docx = out_dir / "template.docx"
pdf = out_dir / "template.pdf"

word = win32com.client.Dispatch("Word.Application")
word.Visible = False
doc = word.Documents.Open(str(docx))
doc.SaveAs(str(pdf), FileFormat=17)  # wdFormatPDF
doc.Close(False)
word.Quit()
print("pdf", pdf)
