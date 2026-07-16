namespace ATG.Platform.Domain.Enums;

public enum OrderPhase
{
    Draft,
    AwaitingDeptHeadApproval,
    NeedsRevision,
    SpecialistCoordination,
    DepartmentCoordination,
    AwaitingLegalApproval,
    AwaitingSupervisingDeputyApproval,
    AwaitingFirstDeputyApproval,
    AwaitingGeneralDirectorApproval,
    EdsFinalized,
    AwaitingRegistration,
    AwaitingPaperSignature,
    AwaitingScanUpload,
    AwaitingDistribution,
    AwaitingArchive,
    Completed
}
