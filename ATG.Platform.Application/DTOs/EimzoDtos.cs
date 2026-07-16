namespace ATG.Platform.Application.DTOs;

public record EimzoPingDto(
    string ServerDateTime,
    string YourIp,
    object? VpnKeyInfo);

public record EimzoStatusDto(
    bool Enabled,
    bool Reachable,
    string? ServerVersion,
    string? ServerTime,
    string? VpnValidTo,
    string? Error);

public record EimzoVerifyResultDto(
    bool Success,
    string? Message,
    string? SignerFullName,
    string? SignerPinpp,
    string? SignerTin,
    string? CertificateSerial,
    string? SigningTime,
    string? DocumentBase64);

public record EimzoTimestampRequest(string Pkcs7Base64);

public record EimzoTimestampResultDto(
    bool Success,
    string? Message,
    string? TimestampedPkcs7Base64);

public record EimzoVerifyAttachedRequest(string Pkcs7Base64);

public record EimzoVerifyDetachedRequest(string DetachedDataBase64, string Pkcs7Base64);

public record EimzoFrontendConfigDto(
    string Domain,
    string ApiKey,
    string TimestampProxyUrl);
