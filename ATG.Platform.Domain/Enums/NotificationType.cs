namespace ATG.Platform.Domain.Enums;

public enum NotificationType
{
    DcsApprovalRequired,
    MarketingPlanApprovalRequired,
    TaskAssigned,
    TicketAssigned,
    DcsApprovalRejected,
    DcsApprovalReminder,
    MarketingPlanApprovalReminder,
    ContractsRoutingRequired,
    ContractsSectionAssigned,
    ContractsEngineerAssigned,
    /// <summary>Request moved to another department/phase — informs initiator and stakeholders.</summary>
    ProcurementPhaseMoved,
    HrBusinessTripCertificateAvailable,
    /// <summary>IT Automation license/service expiry is within the warning window (default 3 months).</summary>
    ItAssetExpiryWarning,
}
