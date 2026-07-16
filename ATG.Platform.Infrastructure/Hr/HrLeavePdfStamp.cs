namespace ATG.Platform.Infrastructure.Hr;

public enum HrLeaveStampStyle
{
    Sent,
    Reviewed,
    Approved,
}

public record HrLeavePdfStamp(
    string Title,
    HrLeaveStampStyle Style,
    string DocumentNumber,
    DateTime SignedAt,
    string SignerName,
    string? SignerPinpp,
    string Operator,
    string? IpAddress);
