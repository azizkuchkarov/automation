namespace ATG.Platform.Domain.Enums;

public enum OutgoingLetterPhase
{
    Draft,
    TranslationPending,
    ReadyForEds,
    AwaitingDeptHeadApproval,
    NeedsRevision,
    SpecialistCoordination,
    DepartmentCoordination,
    AwaitingSupervisingDeputyApproval,
    AwaitingFirstDeputyApproval,
    AwaitingGeneralDirectorApproval,
    EdsFinalized,
    AwaitingRegistration,
    AwaitingPaperSignature,
    AwaitingDispatch,
    AwaitingArchive,
    Completed
}
