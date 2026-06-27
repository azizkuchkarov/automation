using ATG.Platform.Application.DTOs;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Dcs;

public static class ProcurementTopologyBuilder
{
    private const string HoMkt = "HO-MKT";
    private const string HoCproc = "HO-CPROC";
    private const string BmgmcTech = "BMGMC-TECH";

    public static IReadOnlyList<ProcurementTopologyNodeDto> Build(ProcurementRequestDetail detail)
    {
        var activities = detail.Document.Activities.OrderBy(a => a.CreatedAt).ToList();
        var isTas = detail.Flow == ProcurementRequestFlow.TechnicalAffairs;
        var phase = detail.Phase;

        var initiationDone = true;
        var tasDone = !isTas || phase != ProcurementRequestPhase.InProgress || detail.CurrentStep > ProcurementRequestSteps.TotalSteps;
        var tasActive = isTas && phase == ProcurementRequestPhase.InProgress;
        var approvalDone = phase is ProcurementRequestPhase.Marketing
            or ProcurementRequestPhase.Contracts
            or ProcurementRequestPhase.Completed
            || detail.Document.Status == DocumentStatus.Rejected;
        var approvalActive = phase == ProcurementRequestPhase.AwaitingApproval;
        var registered = detail.Document.RegisteredAt is not null;
        var marketingDone = phase is ProcurementRequestPhase.Contracts or ProcurementRequestPhase.Completed
            || (phase == ProcurementRequestPhase.Marketing && detail.MarketingSubPhase == ProcurementMarketingSubPhase.Completed);
        var marketingActive = phase == ProcurementRequestPhase.Marketing && !marketingDone;
        var marketingStep = phase == ProcurementRequestPhase.Marketing
            ? Math.Min(detail.MarketingCurrentStep, MarketingRequestSteps.TotalSteps)
            : 0;
        var marketingAssignee = marketingActive || marketingDone
            ? detail.MarketingSpecialist?.FullName ?? detail.Document.Assignee?.FullName
            : null;
        var marketingCompletedAt = detail.MarketingCompletedAt
            ?? activities.LastOrDefault(a => a.Action is "marketing_completed" or "handoff_contracts")?.CreatedAt;
        var contractsDone = phase == ProcurementRequestPhase.Completed;
        var contractsActive = phase == ProcurementRequestPhase.Contracts;

        var nodes = new List<ProcurementTopologyNodeDto>
        {
            Node(
                "initiation",
                "Инициация заявки",
                "Request initiation",
                detail.InitiatorDepartment?.Code ?? detail.Document.Department.Code,
                detail.InitiatorDepartment?.Name ?? detail.Document.Department.Name,
                detail.InitiatorDepartment?.NameEn ?? detail.Document.Department.NameEn,
                initiationDone ? ProcurementTopologyNodeStatus.Completed : ProcurementTopologyNodeStatus.Active,
                detail.Initiator?.FullName ?? detail.Document.Author.FullName,
                activities.FirstOrDefault(a => a.Action == "created")?.CreatedAt),
        };

        if (isTas)
        {
            nodes.Add(Node(
                "tas_workflow",
                $"Технический процесс (шаг {Math.Min(detail.CurrentStep, ProcurementRequestSteps.TotalSteps)}/{ProcurementRequestSteps.TotalSteps})",
                $"Technical workflow (step {Math.Min(detail.CurrentStep, ProcurementRequestSteps.TotalSteps)}/{ProcurementRequestSteps.TotalSteps})",
                BmgmcTech,
                "Отдел технического обеспечения",
                "Technical Affairs Section",
                tasActive ? ProcurementTopologyNodeStatus.Active
                    : tasDone ? ProcurementTopologyNodeStatus.Completed
                    : ProcurementTopologyNodeStatus.Pending,
                detail.Document.Assignee?.FullName,
                activities.LastOrDefault(a => a.Action == "step_completed")?.CreatedAt));
        }

        nodes.Add(Node(
            "approval",
            "Согласование",
            "Approval",
            detail.Document.Department.Code,
            detail.Document.Department.Name,
            detail.Document.Department.NameEn,
            approvalActive ? ProcurementTopologyNodeStatus.Active
                : approvalDone ? ProcurementTopologyNodeStatus.Completed
                : ProcurementTopologyNodeStatus.Pending,
            detail.Approvers.FirstOrDefault(a => a.Status == ProcurementApproverStatus.Pending)?.User.FullName,
            activities.LastOrDefault(a => a.Action == "approved")?.CreatedAt));

        nodes.Add(Node(
            "registration",
            "Регистрация ATG-REQ",
            "ATG-REQ registration",
            HoCproc,
            "Департамент по контрактам и закупкам",
            "Contracts and Procurement Department",
            registered ? ProcurementTopologyNodeStatus.Completed
                : approvalDone && !registered ? ProcurementTopologyNodeStatus.Pending
                : approvalActive ? ProcurementTopologyNodeStatus.Pending
                : ProcurementTopologyNodeStatus.Pending,
            null,
            detail.Document.RegisteredAt));

        nodes.Add(Node(
            "marketing",
            $"Маркетинг и тендеры (шаг {marketingStep}/{MarketingRequestSteps.TotalSteps})",
            $"Marketing & tenders (step {marketingStep}/{MarketingRequestSteps.TotalSteps})",
            HoMkt,
            "Департамент по маркетингу и управлению тендерами",
            "Marketing and Tender Management Department",
            marketingActive ? ProcurementTopologyNodeStatus.Active
                : marketingDone ? ProcurementTopologyNodeStatus.Completed
                : ProcurementTopologyNodeStatus.Pending,
            marketingAssignee,
            marketingCompletedAt ?? activities.LastOrDefault(a => a.Action == "handoff_marketing")?.CreatedAt));

        nodes.Add(Node(
            "contracts",
            "Департамент контрактов",
            "Contracts department",
            HoCproc,
            "Департамент по контрактам и закупкам",
            "Contracts and Procurement Department",
            contractsActive ? ProcurementTopologyNodeStatus.Active
                : contractsDone ? ProcurementTopologyNodeStatus.Completed
                : ProcurementTopologyNodeStatus.Pending,
            contractsActive || contractsDone ? detail.Document.Assignee?.FullName : null,
            activities.LastOrDefault(a => a.Action == "handoff_contracts")?.CreatedAt));

        return nodes;
    }

    private static ProcurementTopologyNodeDto Node(
        string key,
        string labelRu,
        string labelEn,
        string? deptCode,
        string? deptNameRu,
        string? deptNameEn,
        ProcurementTopologyNodeStatus status,
        string? assignee,
        DateTime? completedAt) =>
        new(key, labelRu, labelEn, deptCode, deptNameRu, deptNameEn, status, assignee, completedAt);
}
