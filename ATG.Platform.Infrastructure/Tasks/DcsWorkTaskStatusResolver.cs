using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Tasks;

public static class DcsWorkTaskStatusResolver
{
    public sealed record ResolvedStatus(
        WorkTaskStatus Status,
        DateTime? StartedAt,
        DateTime? CompletedAt);

    public static ResolvedStatus ResolveResponsible(ProcurementRequestDetail detail)
    {
        if (detail.Document.Status == DocumentStatus.Rejected)
            return new(WorkTaskStatus.Cancelled, null, detail.Document.UpdatedAt);

        if (detail.Phase is ProcurementRequestPhase.Marketing
            or ProcurementRequestPhase.Contracts
            or ProcurementRequestPhase.Completed)
        {
            return new(
                WorkTaskStatus.Done,
                detail.Document.CreatedAt,
                detail.MarketingAssignedAt ?? detail.Document.UpdatedAt);
        }

        if (detail.Phase == ProcurementRequestPhase.AwaitingApproval)
            return new(WorkTaskStatus.InProgress, detail.Document.CreatedAt, null);

        if (detail.Phase == ProcurementRequestPhase.InProgress && detail.CurrentStep > 1)
            return new(WorkTaskStatus.InProgress, detail.Document.CreatedAt, null);

        return new(WorkTaskStatus.New, null, null);
    }

    public static ResolvedStatus ResolveMarketing(ProcurementRequestDetail detail)
    {
        if (detail.Phase is ProcurementRequestPhase.Contracts or ProcurementRequestPhase.Completed)
        {
            return new(
                WorkTaskStatus.Done,
                detail.MarketingAcceptedAt ?? detail.MarketingAssignedAt,
                detail.MarketingCompletedAt ?? detail.Document.UpdatedAt);
        }

        if (detail.Phase != ProcurementRequestPhase.Marketing)
            return new(WorkTaskStatus.New, null, null);

        return detail.MarketingSubPhase switch
        {
            ProcurementMarketingSubPhase.Completed => new(
                WorkTaskStatus.Done,
                detail.MarketingAcceptedAt ?? detail.MarketingAssignedAt,
                detail.MarketingCompletedAt),
            ProcurementMarketingSubPhase.InProgress => new(
                WorkTaskStatus.InProgress,
                detail.MarketingAcceptedAt ?? detail.MarketingAssignedAt,
                null),
            _ => new(WorkTaskStatus.New, null, null),
        };
    }

    public static ResolvedStatus ResolveContracts(ProcurementRequestDetail detail)
    {
        if (detail.Phase == ProcurementRequestPhase.Completed)
        {
            return new(
                WorkTaskStatus.Done,
                detail.ContractsAcceptedAt ?? detail.ContractsAssignedAt,
                detail.Document.UpdatedAt);
        }

        if (detail.Phase != ProcurementRequestPhase.Contracts)
            return new(WorkTaskStatus.New, null, null);

        return detail.ContractsSubPhase switch
        {
            ProcurementContractsSubPhase.Completed => new(
                WorkTaskStatus.Done,
                detail.ContractsAcceptedAt ?? detail.ContractsAssignedAt,
                detail.Document.UpdatedAt),
            ProcurementContractsSubPhase.InProgress => new(
                WorkTaskStatus.InProgress,
                detail.ContractsAcceptedAt ?? detail.ContractsAssignedAt,
                null),
            _ => new(WorkTaskStatus.New, null, null),
        };
    }
}
