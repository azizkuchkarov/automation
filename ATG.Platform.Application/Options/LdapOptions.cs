namespace ATG.Platform.Application.Options;

public class LdapOptions
{
    public const string SectionName = "Ldap";

    public bool Enabled { get; set; } = true;
    public string Server { get; set; } = "DC03.atg.uz";
    public int Port { get; set; } = 389;
    public bool UseSsl { get; set; }
    public string BaseDn { get; set; } = "DC=atg,DC=uz";
    public string BindDn { get; set; } = string.Empty;
    public string BindPassword { get; set; } = string.Empty;
    /// <summary>UPN suffix, e.g. atg.uz — used for username@domain bind.</summary>
    public string Domain { get; set; } = "atg.uz";
    /// <summary>NetBIOS domain name for DOMAIN\user bind, e.g. ATG.</summary>
    public string NetBiosName { get; set; } = "ATG";
}
