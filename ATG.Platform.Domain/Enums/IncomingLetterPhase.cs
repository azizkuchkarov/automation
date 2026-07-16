namespace ATG.Platform.Domain.Enums;

public enum IncomingLetterPhase
{
    Received,
    TranslationPending,
    ReadyForRegistration,
    Registered,
    AwaitingResolution,
    RoutedToDepartment,
    AwaitingAcceptance,
    InExecution,
    AwaitingReview,
    NeedsRevision,
    AwaitingArchive,
    Completed
}
