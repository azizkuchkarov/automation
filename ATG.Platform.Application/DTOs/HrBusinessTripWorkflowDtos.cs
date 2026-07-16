namespace ATG.Platform.Application.DTOs;

using ATG.Platform.Domain.Enums;

public record HrBusinessTripWorkflowApproverStep(
    HrBusinessTripApprovalRole Role,
    Guid UserId);

public record HrBusinessTripWorkflowPersonDto(
    Guid UserId,
    string FullName,
    string Email);

public record HrBusinessTripWorkflowStepDto(
    Guid Id,
    int SortOrder,
    Guid ApproverUserId,
    string ApproverName,
    string ApproverEmail,
    string Role,
    string? LabelRu,
    string? LabelEn);

public record HrBusinessTripWorkflowTierDto(
    Guid Id,
    string TierKey,
    string TitleRu,
    string TitleEn,
    int MatchPriority,
    bool CatchAllStaff,
    bool PrependsSectionManager,
    IReadOnlyList<HrBusinessTripWorkflowPersonDto> Initiators,
    IReadOnlyList<HrBusinessTripWorkflowStepDto> Steps);

public record HrBusinessTripDeptWorkflowDto(
    Guid Id,
    string DepartmentCode,
    string TitleRu,
    string TitleEn,
    IReadOnlyList<HrBusinessTripWorkflowTierDto> Tiers);

public record HrBusinessTripWorkflowAdminDto(
    IReadOnlyList<HrBusinessTripDeptWorkflowDto> Departments,
    string? OrganizationName);
