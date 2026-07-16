# -*- coding: utf-8 -*-
import sys
from pathlib import Path

src = Path(r"C:\Users\a.kuchkarov\Downloads") / "фирм бланк приложение.doc"
out_dir = Path(r"C:\Users\a.kuchkarov\Desktop\automation\.temp-query\prikaz-template")
out_dir.mkdir(parents=True, exist_ok=True)

# Try Word COM first for .doc
try:
    import win32com.client  # type: ignore
    word = win32com.client.Dispatch("Word.Application")
    word.Visible = False
    doc = word.Documents.Open(str(src))
    # export full text
    txt = doc.Content.Text
    (out_dir / "full.txt").write_text(txt, encoding="utf-8")
    # page by page via export to PDF then PyMuPDF, or SaveAs docx
    docx = out_dir / "template.docx"
    doc.SaveAs(str(docx), FileFormat=16)  # wdFormatXMLDocument
    doc.Close(False)
    word.Quit()
    print("saved", docx)
except Exception as e:
    print("COM failed:", e)
    sys.exit(1)
