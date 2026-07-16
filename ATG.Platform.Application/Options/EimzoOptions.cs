namespace ATG.Platform.Application.Options;

public class EimzoOptions
{
    public const string SectionName = "Eimzo";

    public bool Enabled { get; set; } = true;
    public string ServerBaseUrl { get; set; } = "http://localhost:8080";
    public string SiteHost { get; set; } = "unilogin.atg.uz";
    public string? FrontendApiKey { get; set; }
    /// <summary>Public IP registered for the E-IMZO API key (see GET /ping on E-IMZO-SERVER).</summary>
    public string? ClientIp { get; set; }
    /// <summary>When true, timestamp failures fall back to unsigned PKCS#7 (local dev only).</summary>
    public bool AllowUnsignedPkcs7 { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
