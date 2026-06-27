using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using ATG.Platform.Application.Common;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATG.Platform.Infrastructure.Services;

public class LdapService(IOptions<LdapOptions> options, ILogger<LdapService> logger) : ILdapService
{
    private readonly LdapOptions _options = options.Value;

    public Task<Result<string>> AuthenticateAsync(string login, string password, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return Task.FromResult(Result<string>.Fail("LDAP is disabled"));

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return Task.FromResult(Result<string>.Fail("Invalid email or password"));

        try
        {
            var loginId = login.Trim();

            if (!string.IsNullOrWhiteSpace(_options.BindDn))
            {
                var email = AuthenticateWithServiceAccount(loginId, password);
                return Task.FromResult(email is null
                    ? Result<string>.Fail("Invalid email or password")
                    : Result<string>.Ok(email));
            }

            var directEmail = TryDirectBind(loginId, password);
            return Task.FromResult(directEmail is null
                ? Result<string>.Fail("Invalid email or password")
                : Result<string>.Ok(directEmail));
        }
        catch (LdapException ex)
        {
            logger.LogWarning(ex, "LDAP error during authentication for {Login}", login);
            return Task.FromResult(Result<string>.Fail("Authentication service unavailable"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected LDAP error for {Login}", login);
            return Task.FromResult(Result<string>.Fail("Authentication service unavailable"));
        }
    }

    private string? AuthenticateWithServiceAccount(string loginId, string password)
    {
        using var searchConnection = CreateConnection();
        searchConnection.Credential = new NetworkCredential(_options.BindDn, _options.BindPassword);
        searchConnection.AuthType = AuthType.Basic;
        searchConnection.Bind();

        var userDn = FindUserDn(searchConnection, loginId);
        if (userDn is null) return null;

        var email = FindUserEmail(searchConnection, loginId);
        if (email is null) return null;

        using var userConnection = CreateConnection();
        userConnection.Credential = new NetworkCredential(userDn, password);
        userConnection.AuthType = AuthType.Basic;
        userConnection.Bind();

        return email;
    }

    private string? TryDirectBind(string loginId, string password)
    {
        foreach (var identity in GetBindIdentities(loginId))
        {
            try
            {
                using var connection = CreateConnection();
                connection.Credential = new NetworkCredential(identity, password);
                connection.AuthType = AuthType.Negotiate;
                connection.Bind();
                return ResolveEmail(loginId, identity);
            }
            catch (LdapException ex) when (ex.ErrorCode is 49 or 50)
            {
                logger.LogDebug("LDAP bind rejected for identity {Identity}", identity);
            }
        }

        return null;
    }

    private string? FindUserDn(LdapConnection connection, string loginId)
    {
        var response = Search(connection, loginId, "distinguishedName");
        return response?.Entries.Count > 0
            ? response.Entries[0].DistinguishedName
            : null;
    }

    private string? FindUserEmail(LdapConnection connection, string loginId)
    {
        var response = Search(connection, loginId, "mail", "userPrincipalName");
        if (response?.Entries.Count == 0) return null;

        var entry = response!.Entries[0];
        var mail = GetAttributeValue(entry, "mail");
        if (!string.IsNullOrWhiteSpace(mail)) return mail.ToLowerInvariant();

        var upn = GetAttributeValue(entry, "userPrincipalName");
        return string.IsNullOrWhiteSpace(upn) ? null : upn.ToLowerInvariant();
    }

    private SearchResponse? Search(LdapConnection connection, string loginId, params string[] attributes)
    {
        var filter = BuildUserFilter(loginId);
        var request = new SearchRequest(_options.BaseDn, filter, SearchScope.Subtree, attributes);
        return (SearchResponse)connection.SendRequest(request);
    }

    private static string BuildUserFilter(string loginId)
    {
        var escaped = EscapeLdapFilter(loginId);
        if (loginId.Contains('@'))
            return $"(&(objectClass=user)(|(userPrincipalName={escaped})(mail={escaped})))";

        return $"(&(objectClass=user)(|(sAMAccountName={escaped})(userPrincipalName={escaped})(mail={escaped})))";
    }

    private IEnumerable<string> GetBindIdentities(string loginId)
    {
        var identities = new List<string>();

        if (loginId.Contains('@'))
            identities.Add(loginId);

        if (!loginId.Contains('@') && !string.IsNullOrEmpty(_options.Domain))
            identities.Add($"{loginId}@{_options.Domain}");

        if (!loginId.Contains('@') && !string.IsNullOrEmpty(_options.NetBiosName))
            identities.Add($"{_options.NetBiosName}\\{loginId}");

        return identities.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private string ResolveEmail(string loginId, string bindIdentity)
    {
        if (loginId.Contains('@')) return loginId.ToLowerInvariant();
        if (bindIdentity.Contains('@')) return bindIdentity.ToLowerInvariant();
        if (!string.IsNullOrEmpty(_options.Domain))
            return $"{loginId}@{_options.Domain}".ToLowerInvariant();
        return loginId.ToLowerInvariant();
    }

    private LdapConnection CreateConnection()
    {
        var identifier = new LdapDirectoryIdentifier(_options.Server, _options.Port);
        var connection = new LdapConnection(identifier) { Timeout = TimeSpan.FromSeconds(10) };
        connection.SessionOptions.ProtocolVersion = 3;
        if (_options.UseSsl)
            connection.SessionOptions.SecureSocketLayer = true;
        return connection;
    }

    private static string? GetAttributeValue(SearchResultEntry entry, string name)
    {
        if (!entry.Attributes.Contains(name)) return null;
        var values = entry.Attributes[name];
        return values?.Count > 0 ? values[0]?.ToString() : null;
    }

    private static string EscapeLdapFilter(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\5c"); break;
                case '*': sb.Append("\\2a"); break;
                case '(': sb.Append("\\28"); break;
                case ')': sb.Append("\\29"); break;
                case '\0': sb.Append("\\00"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
