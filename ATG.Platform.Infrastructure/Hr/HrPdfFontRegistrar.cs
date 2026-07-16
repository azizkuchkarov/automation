using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrPdfFontRegistrar
{
    private static readonly object Gate = new();
    private static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered) return;

        lock (Gate)
        {
            if (_registered) return;

            QuestPDF.Settings.License = LicenseType.Community;
            RegisterEmbedded("ATG.Platform.Infrastructure.Hr.Assets.Fonts.arial.ttf");
            RegisterEmbedded("ATG.Platform.Infrastructure.Hr.Assets.Fonts.arialbd.ttf");
            _registered = true;
        }
    }

    private static void RegisterEmbedded(string resourceName)
    {
        var assembly = typeof(HrPdfFontRegistrar).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded font not found: {resourceName}");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        FontManager.RegisterFont(new MemoryStream(memory.ToArray()));
    }
}
