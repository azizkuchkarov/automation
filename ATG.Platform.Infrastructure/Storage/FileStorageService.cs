using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace ATG.Platform.Infrastructure.Storage;

public class FileStorageService(IOptions<MinioOptions> options, ILogger<FileStorageService> logger) : IFileStorageService
{
    private readonly MinioOptions _opt = options.Value;
    private readonly string _localRoot = Path.Combine(AppContext.BaseDirectory, "uploads");

    public async Task<string> UploadAsync(string folder, string fileName, Stream content, string contentType, CancellationToken ct = default)
    {
        var safeName = Path.GetFileName(fileName);
        var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}_{safeName}";

        if (_opt.Enabled)
        {
            var client = CreateClient();
            await EnsureBucketAsync(client, ct);
            var bytes = await ReadAllAsync(content, ct);
            using var ms = new MemoryStream(bytes);
            await client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_opt.Bucket)
                .WithObject(key)
                .WithStreamData(ms)
                .WithObjectSize(ms.Length)
                .WithContentType(contentType), ct);
            return key;
        }

        Directory.CreateDirectory(Path.Combine(_localRoot, folder));
        var path = Path.Combine(_localRoot, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var fs = File.Create(path);
        await content.CopyToAsync(fs, ct);
        logger.LogInformation("Stored file locally: {Key}", key);
        return key;
    }

    public async Task<(Stream Stream, string ContentType)?> DownloadAsync(string key, CancellationToken ct = default)
    {
        if (_opt.Enabled)
        {
            var client = CreateClient();
            var ms = new MemoryStream();
            await client.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_opt.Bucket)
                .WithObject(key)
                .WithCallbackStream(s => s.CopyTo(ms)), ct);
            ms.Position = 0;
            return (ms, GuessContentType(key));
        }

        var path = Path.Combine(_localRoot, key.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) return null;
        return (File.OpenRead(path), GuessContentType(key));
    }

    public string? GetPublicUrl(string key) =>
        _opt.Enabled && !string.IsNullOrWhiteSpace(_opt.PublicBaseUrl)
            ? $"{_opt.PublicBaseUrl.TrimEnd('/')}/{_opt.Bucket}/{key}"
            : null;

    private IMinioClient CreateClient() =>
        new MinioClient()
            .WithEndpoint(_opt.Endpoint)
            .WithCredentials(_opt.AccessKey, _opt.SecretKey)
            .WithSSL(_opt.UseSsl)
            .Build();

    private async Task EnsureBucketAsync(IMinioClient client, CancellationToken ct)
    {
        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_opt.Bucket), ct);
        if (!exists) await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_opt.Bucket), ct);
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    private static string GuessContentType(string key) =>
        Path.GetExtension(key).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream",
        };
}
