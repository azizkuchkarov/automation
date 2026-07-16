namespace ATG.Platform.Infrastructure.Dcs;

public record ContractsDomStepDefinition(
    int Number,
    bool HasBranch,
    string TitleRu,
    string TitleEn,
    string HintRu,
    string HintEn,
    string? BranchHintRu,
    string? BranchHintEn,
    bool RequiresUpload = false,
    bool RequiresApprovers = false,
    bool RequiresContractsAdmin = false,
    bool RequiresRegistration = false,
    bool RequiresScheduleDate = false,
    string? ScheduleLabelRu = null,
    string? ScheduleLabelEn = null,
    string? ScheduleHintRu = null,
    string? ScheduleHintEn = null,
    bool AllowsReturnToMarketing = false,
    bool AllowsTerminationRollback = false,
    int? RollbackStepNumber = null);
