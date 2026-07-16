using NPOI.XWPF.UserModel;

namespace ATG.Platform.Infrastructure.Dcs;

internal static class XwpfTextReplacer
{
    public static void ReplaceAll(XWPFDocument doc, string search, string replacement, Action<XWPFRun>? styleFirstChangedRun = null)
    {
        if (string.IsNullOrEmpty(search) || search == replacement)
            return;

        foreach (var paragraph in doc.Paragraphs)
            ReplaceAllInParagraph(paragraph, search, replacement, styleFirstChangedRun);

        foreach (var table in doc.Tables)
            ReplaceInTable(table, search, replacement, styleFirstChangedRun);

        foreach (var header in doc.HeaderList)
            ReplaceInHeaderFooter(header, search, replacement, styleFirstChangedRun);

        foreach (var footer in doc.FooterList)
            ReplaceInHeaderFooter(footer, search, replacement, styleFirstChangedRun);
    }

    private static void ReplaceInHeaderFooter(XWPFHeaderFooter headerFooter, string search, string replacement, Action<XWPFRun>? styleFirstChangedRun)
    {
        foreach (var paragraph in headerFooter.Paragraphs)
            ReplaceAllInParagraph(paragraph, search, replacement, styleFirstChangedRun);

        foreach (var table in headerFooter.Tables)
            ReplaceInTable(table, search, replacement, styleFirstChangedRun);
    }

    private static void ReplaceInTable(XWPFTable table, string search, string replacement, Action<XWPFRun>? styleFirstChangedRun)
    {
        foreach (var row in table.Rows)
        {
            foreach (var cell in row.GetTableCells())
            {
                foreach (var paragraph in cell.Paragraphs)
                    ReplaceAllInParagraph(paragraph, search, replacement, styleFirstChangedRun);

                foreach (var nested in cell.Tables)
                    ReplaceInTable(nested, search, replacement, styleFirstChangedRun);
            }
        }
    }

    private static void ReplaceAllInParagraph(
        XWPFParagraph paragraph, string search, string replacement, Action<XWPFRun>? styleFirstChangedRun)
    {
        while (ReplaceOnceInParagraph(paragraph, search, replacement, styleFirstChangedRun)) { }
    }

    private static bool ReplaceOnceInParagraph(
        XWPFParagraph paragraph, string search, string replacement, Action<XWPFRun>? styleFirstChangedRun)
    {
        var runs = paragraph.Runs;
        if (runs is null || runs.Count == 0)
            return false;

        var full = paragraph.ParagraphText;
        var startIndex = full.IndexOf(search, StringComparison.Ordinal);
        if (startIndex < 0)
            return false;

        var endIndex = startIndex + search.Length;
        var charIndex = 0;
        var startRun = -1;
        var endRun = -1;
        var startRunOffset = 0;
        var endRunOffset = 0;

        for (var i = 0; i < runs.Count; i++)
        {
            var runText = runs[i].Text ?? string.Empty;
            var runCharStart = charIndex;
            var runCharEnd = charIndex + runText.Length;

            if (startRun < 0 && startIndex >= runCharStart && startIndex < runCharEnd)
            {
                startRun = i;
                startRunOffset = startIndex - runCharStart;
            }

            if (endIndex > runCharStart && endIndex <= runCharEnd)
            {
                endRun = i;
                endRunOffset = endIndex - runCharStart;
                break;
            }

            charIndex = runCharEnd;
        }

        if (startRun < 0 || endRun < 0)
            return false;

        if (startRun == endRun)
        {
            var text = runs[startRun].Text ?? string.Empty;
            runs[startRun].SetText(text[..startRunOffset] + replacement + text[endRunOffset..], 0);
            styleFirstChangedRun?.Invoke(runs[startRun]);
            return true;
        }

        var firstText = runs[startRun].Text ?? string.Empty;
        runs[startRun].SetText(firstText[..startRunOffset] + replacement, 0);
        styleFirstChangedRun?.Invoke(runs[startRun]);

        for (var i = startRun + 1; i < endRun; i++)
            runs[i].SetText(string.Empty, 0);

        var lastText = runs[endRun].Text ?? string.Empty;
        runs[endRun].SetText(lastText[endRunOffset..], 0);
        return true;
    }
}
