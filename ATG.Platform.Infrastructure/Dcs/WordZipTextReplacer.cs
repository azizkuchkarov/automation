using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ATG.Platform.Infrastructure.Dcs;

public static class WordZipTextReplacer
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public static byte[] FillTemplate(string templatePath, IReadOnlyList<(string Search, string Replace)> replacements)
    {
        using var input = File.OpenRead(templatePath);
        using var output = new MemoryStream();

        using (var src = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: false))
        using (var dest = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in src.Entries)
            {
                var destEntry = dest.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                using var srcStream = entry.Open();
                using var destStream = destEntry.Open();

                if (ShouldProcessPart(entry.FullName))
                {
                    using var reader = new StreamReader(srcStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    var xml = reader.ReadToEnd();
                    foreach (var (search, replace) in replacements)
                        xml = ReplaceAcrossTextNodes(xml, search, replace);
                    var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(xml);
                    destStream.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    srcStream.CopyTo(destStream);
                }
            }
        }

        return output.ToArray();
    }

    private static bool ShouldProcessPart(string name) =>
        name.StartsWith("word/", StringComparison.OrdinalIgnoreCase)
        && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
        && !name.Contains("_rels", StringComparison.OrdinalIgnoreCase);

    private static string ReplaceAcrossTextNodes(string xml, string search, string replacement)
    {
        if (string.IsNullOrEmpty(search) || search == replacement)
            return xml;

        var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var textNodes = doc.Descendants(W + "t").ToList();
        if (textNodes.Count == 0)
            return xml;

        var changed = false;
        while (ReplaceOnceInTextNodes(textNodes, search, replacement))
            changed = true;

        if (!changed)
            return xml;

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            OmitXmlDeclaration = false,
            CloseOutput = false,
        }))
        {
            doc.Save(writer);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static bool ReplaceOnceInTextNodes(List<XElement> textNodes, string search, string replacement)
    {
        var texts = textNodes.Select(n => n.Value).ToList();
        var full = string.Concat(texts);
        var startIndex = full.IndexOf(search, StringComparison.Ordinal);
        if (startIndex < 0)
            return false;

        var endIndex = startIndex + search.Length;
        var charIndex = 0;
        var startNode = -1;
        var endNode = -1;
        var startOffset = 0;
        var endOffset = 0;

        for (var i = 0; i < textNodes.Count; i++)
        {
            var nodeText = texts[i];
            var nodeStart = charIndex;
            var nodeEnd = charIndex + nodeText.Length;

            if (startNode < 0 && startIndex >= nodeStart && startIndex < nodeEnd)
            {
                startNode = i;
                startOffset = startIndex - nodeStart;
            }

            if (endIndex > nodeStart && endIndex <= nodeEnd)
            {
                endNode = i;
                endOffset = endIndex - nodeStart;
                break;
            }

            charIndex = nodeEnd;
        }

        if (startNode < 0 || endNode < 0)
            return false;

        if (startNode == endNode)
        {
            var text = texts[startNode];
            textNodes[startNode].Value = text[..startOffset] + replacement + text[endOffset..];
            return true;
        }

        textNodes[startNode].Value = texts[startNode][..startOffset] + replacement;
        for (var i = startNode + 1; i < endNode; i++)
            textNodes[i].Value = string.Empty;
        textNodes[endNode].Value = texts[endNode][endOffset..];
        return true;
    }
}
