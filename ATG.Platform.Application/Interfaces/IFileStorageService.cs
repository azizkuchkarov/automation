namespace ATG.Platform.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(string folder, string fileName, Stream content, string contentType, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType)?> DownloadAsync(string key, CancellationToken ct = default);
    string? GetPublicUrl(string key);
}
