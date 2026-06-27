namespace ATG.Platform.Application.Options;

public class MinioOptions
{
    public const string SectionName = "Minio";
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Bucket { get; set; } = "atg-marketing";
    public bool UseSsl { get; set; }
    public string? PublicBaseUrl { get; set; }
}
