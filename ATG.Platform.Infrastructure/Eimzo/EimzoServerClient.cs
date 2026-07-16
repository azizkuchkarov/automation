using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATG.Platform.Infrastructure.Eimzo;

public class EimzoServerClient(
    HttpClient http,
    IOptions<EimzoOptions> options,
    ILogger<EimzoServerClient> logger) : IEimzoServerClient
{
    private static readonly string[] PinppOids =
    [
        "1.2.860.3.16.1.2",
        "1.3.6.1.4.1.64375.1.2",
        "PINFL",
        "UID",
    ];

    private static readonly string[] TinOids =
    [
        "1.2.860.3.16.1.1",
        "1.3.6.1.4.1.64375.1.1",
    ];

    private readonly EimzoOptions _options = options.Value;

    public async Task<EimzoStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return new EimzoStatusDto(false, false, null, null, null, "E-IMZO is disabled");

        try
        {
            var infoJson = await GetAsync("/info", null, ct);
            var info = JsonNode.Parse(infoJson) as JsonObject;
            var version = info?["version"]?.GetValue<string>();
            var serverTime = info?["serverTime"]?.GetValue<string>();
            var validTo = info?["vpnKeyInfo"]?["validTo"]?.GetValue<string>();
            return new EimzoStatusDto(true, true, version, serverTime, validTo, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "E-IMZO-SERVER unreachable at {BaseUrl}", _options.ServerBaseUrl);
            return new EimzoStatusDto(true, false, null, null, null, ex.Message);
        }
    }

    public Task<Result<EimzoVerifyResultDto>> VerifyAttachedAsync(
        string pkcs7Base64, string clientIp, CancellationToken ct = default) =>
        VerifyAsync("/backend/pkcs7/verify/attached", pkcs7Base64.Trim(), clientIp, ct);

    public Task<Result<EimzoVerifyResultDto>> VerifyDetachedAsync(
        string detachedDataBase64, string pkcs7Base64, string clientIp, CancellationToken ct = default) =>
        VerifyAsync("/backend/pkcs7/verify/detached", $"{detachedDataBase64.Trim()}|{pkcs7Base64.Trim()}", clientIp, ct);

    public async Task<Result<EimzoTimestampResultDto>> AttachTimestampAsync(
        string pkcs7Base64, string clientIp, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return Result<EimzoTimestampResultDto>.Fail("E-IMZO is disabled");

        var normalizedPkcs7 = pkcs7Base64.Trim();
        var resolvedIp = ResolveClientIp(clientIp);

        try
        {
            var (statusCode, body) = await PostRawAsync("/frontend/timestamp/pkcs7", normalizedPkcs7, resolvedIp, ct);
            if (statusCode is >= 200 and < 300)
            {
                var json = JsonNode.Parse(body) as JsonObject;
                var status = json?["status"]?.GetValue<int>() ?? 0;
                if (status == 1)
                {
                    return Result<EimzoTimestampResultDto>.Ok(new EimzoTimestampResultDto(
                        true,
                        null,
                        json?["pkcs7b64"]?.GetValue<string>()));
                }

                var message = json?["message"]?.GetValue<string>() ?? "Timestamp failed";
                return MaybeFallbackTimestamp(normalizedPkcs7, message);
            }

            var httpMessage = ParseErrorMessage(body) ?? $"E-IMZO timestamp HTTP {statusCode}";
            logger.LogWarning("E-IMZO timestamp failed ({Status}): {Message} (client IP: {ClientIp})",
                statusCode, httpMessage, resolvedIp);
            return MaybeFallbackTimestamp(normalizedPkcs7, httpMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "E-IMZO timestamp request failed (client IP: {ClientIp})", resolvedIp);
            return MaybeFallbackTimestamp(normalizedPkcs7, ex.Message);
        }
    }

    private Result<EimzoTimestampResultDto> MaybeFallbackTimestamp(string pkcs7, string message)
    {
        if (!_options.AllowUnsignedPkcs7)
            return Result<EimzoTimestampResultDto>.Fail(message);

        logger.LogWarning("Using unsigned PKCS#7 (timestamp skipped): {Message}", message);
        return Result<EimzoTimestampResultDto>.Ok(new EimzoTimestampResultDto(true, message, pkcs7));
    }

    private async Task<Result<EimzoVerifyResultDto>> VerifyAsync(
        string path, string body, string clientIp, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<EimzoVerifyResultDto>.Fail("E-IMZO is disabled");

        var resolvedIp = ResolveClientIp(clientIp);

        try
        {
            var (statusCode, responseBody) = await PostRawAsync(path, body, resolvedIp, ct);
            if (statusCode is < 200 or >= 300)
            {
                var httpMessage = ParseErrorMessage(responseBody) ?? $"E-IMZO verify HTTP {statusCode}";
                logger.LogWarning("E-IMZO verify failed for {Path} ({Status}): {Message} (client IP: {ClientIp})",
                    path, statusCode, httpMessage, resolvedIp);
                return Result<EimzoVerifyResultDto>.Fail(httpMessage);
            }

            var json = JsonNode.Parse(responseBody) as JsonObject;
            var status = json?["status"]?.GetValue<int>() ?? 0;
            if (status != 1)
            {
                var message = json?["message"]?.GetValue<string>() ?? $"Verification failed (status {status})";
                return Result<EimzoVerifyResultDto>.Fail(message);
            }

            return Result<EimzoVerifyResultDto>.Ok(ParseVerifyResult(json));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "E-IMZO verify request failed for {Path} (client IP: {ClientIp})", path, resolvedIp);
            return Result<EimzoVerifyResultDto>.Fail(ex.Message);
        }
    }

    private async Task<string> GetAsync(string path, string? clientIp, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path.TrimStart('/'));
        ApplyHeaders(request, ResolveClientIp(clientIp));
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private async Task<(int StatusCode, string Body)> PostRawAsync(
        string path, string body, string clientIp, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path.TrimStart('/'))
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        ApplyHeaders(request, clientIp);
        using var response = await http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        return ((int)response.StatusCode, responseBody);
    }

    private void ApplyHeaders(HttpRequestMessage request, string clientIp)
    {
        request.Headers.TryAddWithoutValidation("Host", _options.SiteHost);
        request.Headers.TryAddWithoutValidation("X-Real-IP", clientIp);
    }

    private string ResolveClientIp(string? clientIp)
    {
        if (!string.IsNullOrWhiteSpace(_options.ClientIp))
            return _options.ClientIp.Trim();

        if (!IsPublicClientIp(clientIp))
            return "127.0.0.1";

        return clientIp!.Trim();
    }

    private static bool IsPublicClientIp(string? clientIp)
    {
        if (string.IsNullOrWhiteSpace(clientIp))
            return false;

        if (!IPAddress.TryParse(clientIp, out var address))
            return false;

        if (IPAddress.IsLoopback(address))
            return false;

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        var bytes = address.GetAddressBytes();
        if (bytes.Length == 4)
        {
            if (bytes[0] == 10) return false;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;
            if (bytes[0] == 192 && bytes[1] == 168) return false;
            if (bytes[0] == 127) return false;
        }

        return true;
    }

    private static string? ParseErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            var json = JsonNode.Parse(body) as JsonObject;
            return json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>();
        }
        catch
        {
            return body.Length > 300 ? body[..300] : body;
        }
    }

    private static EimzoVerifyResultDto ParseVerifyResult(JsonObject? json)
    {
        var pkcs7Info = json?["pkcs7Info"] as JsonObject;
        var signers = pkcs7Info?["signers"] as JsonArray;
        var signer = signers?.FirstOrDefault() as JsonObject;
        var subjectInfo = signer?["certificate"]?.AsArray()?.FirstOrDefault()?["subjectInfo"] as JsonObject
            ?? signer?["certificate"] as JsonObject
            ?? signer?["certificate"]?.AsArray()?.FirstOrDefault() as JsonObject;

        if (subjectInfo is null && signer?["certificate"] is JsonArray certArray && certArray.Count > 0)
            subjectInfo = certArray[0]?["subjectInfo"] as JsonObject;

        string? cn = null;
        string? pinpp = null;
        string? tin = null;
        string? serial = null;

        if (signer?["certificate"] is JsonArray certs && certs.Count > 0)
        {
            var cert = certs[0] as JsonObject;
            subjectInfo = cert?["subjectInfo"] as JsonObject;
            serial = cert?["serialNumber"]?.GetValue<string>();
        }

        if (subjectInfo is not null)
        {
            cn = subjectInfo["CN"]?.GetValue<string>();
            pinpp = ReadOid(subjectInfo, PinppOids);
            tin = ReadOid(subjectInfo, TinOids);
        }

        var signingTime = signer?["signingTime"]?.GetValue<string>();
        var documentBase64 = pkcs7Info?["documentBase64"]?.GetValue<string>();

        return new EimzoVerifyResultDto(
            true,
            null,
            cn,
            pinpp,
            tin,
            serial,
            signingTime,
            documentBase64);
    }

    private static string? ReadOid(JsonObject subject, IEnumerable<string> oids)
    {
        foreach (var oid in oids)
        {
            var value = subject[oid]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }
}
