namespace ATG.Platform.Application.Interfaces;

public interface IMarketingRfqChannelService
{
    Task NotifyHelpDeskTicketClosedAsync(Guid ticketId, CancellationToken ct = default);
    Task NotifyWorkTaskCompletedAsync(Guid workTaskId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> ValidateStep3RfqCompletionAsync(Guid documentId, CancellationToken ct = default);
    Task CompleteTenderChannelByEngineerAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
}
