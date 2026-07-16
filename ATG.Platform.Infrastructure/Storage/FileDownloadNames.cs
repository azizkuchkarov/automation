namespace ATG.Platform.Infrastructure.Storage;

public static class FileDownloadNames
{
    /// <summary>
    /// Prefer the original client file name; otherwise strip the storage-key GUID prefix.
    /// Storage keys are stored as "{folder}/{32hex}_{originalName}".
    /// </summary>
    public static string Resolve(string storageKey, string? originalFileName = null)
    {
        if (!string.IsNullOrWhiteSpace(originalFileName))
            return Path.GetFileName(originalFileName.Trim());

        var name = Path.GetFileName(storageKey);
        if (name.Length > 33 && name[32] == '_')
        {
            var prefix = name.AsSpan(0, 32);
            var allHex = true;
            foreach (var c in prefix)
            {
                if (!Uri.IsHexDigit(c))
                {
                    allHex = false;
                    break;
                }
            }
            if (allHex) return name[33..];
        }

        return name;
    }
}
