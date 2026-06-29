using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.HelpDesk;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class MarketingRfqChannelService(AppDbContext db) : IMarketingRfqChannelService
{
    private const string HoItdig = "HO-ITDIG";
    private const string HoMktTnd = "HO-MKT-TND";
    private static readonly string[] TenderOfficerEmails = ["i.kogay@atg.uz", "s.kim@atg.uz"];

    public async Task NotifyHelpDeskTicketClosedAsync(Guid ticketId, CancellationToken ct = default)
    {
        var channel = await db.MarketingRfqChannelRequests
            .Include(c => c.Record)
            .FirstOrDefaultAsync(c => c.HelpDeskTicketId == ticketId && c.Status == MarketingRfqChannelStatus.Open, ct);
        if (channel is null) return;

        await CompleteChannelAsync(channel, ct);
    }

    public async Task NotifyWorkTaskCompletedAsync(Guid workTaskId, CancellationToken ct = default)
    {
        var channel = await db.MarketingRfqChannelRequests
            .Include(c => c.Record)
            .FirstOrDefaultAsync(c => c.WorkTaskId == workTaskId && c.Status == MarketingRfqChannelStatus.Open, ct);
        if (channel is null) return;

        await CompleteChannelAsync(channel, ct);
    }

    public async Task<(bool Ok, string? Error)> ValidateStep4CompletionAsync(Guid documentId, CancellationToken ct = default)
    {
        var record = await db.MarketingRecords.AsNoTracking()
            .Include(r => r.RfqChannelRequests)
            .FirstOrDefaultAsync(r => r.DocumentId == documentId, ct);
        if (record is null)
            return (false, "Marketing record not found");

        if (string.IsNullOrWhiteSpace(record.RfqDocumentStorageKey))
            return (false, "Upload the RFQ document before completing step 4");

        if (!record.RfqChannelRequests.Any())
            return (false, "Open at least one channel: ATG Website or Tenderweek");

        if (record.RfqChannelRequests.Any(c => c.Status == MarketingRfqChannelStatus.Open))
            return (false, "All ATG Website and Tender requests must be closed before completing step 4");

        return (true, null);
    }

    public async Task<MarketingRfqChannelRequest> CreateAtgWebsiteChannelAsync(
        MarketingRecord record, User actor, CancellationToken ct)
    {
        EnsureStep4(record);
        EnsureRfqDocument(record);

        if (record.RfqChannelRequests.Any(c =>
                c.Channel == MarketingRfqChannelType.AtgWebsite && c.Status == MarketingRfqChannelStatus.Open))
            throw new InvalidOperationException("ATG Website request is already open");

        var orgCode = actor.Organization?.Code
            ?? await db.Organizations.AsNoTracking()
                .Where(o => o.Id == actor.OrganizationId)
                .Select(o => o.Code)
                .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("User organization not found");

        var deptCode = HelpDeskRouting.ResolveDepartmentCode(TicketCategory.IT, orgCode)
            ?? throw new InvalidOperationException("IT department routing not found");

        var dept = await db.Departments.FirstOrDefaultAsync(d =>
            d.OrganizationId == actor.OrganizationId && d.Code == deptCode && d.IsActive, ct)
            ?? await db.Departments.FirstOrDefaultAsync(d => d.Code == deptCode && d.IsActive, ct)
            ?? throw new InvalidOperationException($"Department {deptCode} not found");

        var docNumber = record.Request?.Document?.Number ?? record.DocumentId.ToString();
        var ticketNumber = await GenerateTicketNumberAsync(ct);
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Number = ticketNumber,
            Title = $"RFQ publication on ATG Website — {docNumber}",
            Description = BuildAtgWebsiteDescription(record, docNumber),
            Category = TicketCategory.IT,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            RequesterId = actor.Id,
            OrganizationId = actor.OrganizationId,
            TargetDepartmentId = dept.Id,
        };
        db.Tickets.Add(ticket);

        var channel = new MarketingRfqChannelRequest
        {
            Id = Guid.NewGuid(),
            MarketingRecordId = record.Id,
            DocumentId = record.DocumentId,
            Channel = MarketingRfqChannelType.AtgWebsite,
            Status = MarketingRfqChannelStatus.Open,
            HelpDeskTicketId = ticket.Id,
            ExternalNumber = ticket.Number,
            CreatedById = actor.Id,
        };
        db.MarketingRfqChannelRequests.Add(channel);
        record.RfqChannelRequests.Add(channel);
        record.UpdatedAt = DateTime.UtcNow;

        return channel;
    }

    public async Task<MarketingRfqChannelRequest> CreateTenderChannelAsync(
        MarketingRecord record, User actor, CancellationToken ct)
    {
        EnsureStep4(record);
        EnsureRfqDocument(record);

        if (record.RfqChannelRequests.Any(c =>
                c.Channel == MarketingRfqChannelType.Tenderweek && c.Status == MarketingRfqChannelStatus.Open))
            throw new InvalidOperationException("Tenderweek request is already open");

        var assignee = await ResolveTenderOfficerAsync(ct)
            ?? throw new InvalidOperationException("Tender officer not found");

        var docNumber = record.Request?.Document?.Number ?? record.DocumentId.ToString();
        var task = new WorkTask
        {
            Id = Guid.NewGuid(),
            Number = await GenerateTaskNumberAsync(ct),
            Title = $"Tenderweek RFQ — {docNumber}",
            Description = BuildTenderDescription(record, docNumber),
            Status = WorkTaskStatus.New,
            Priority = record.Request?.Priority ?? TaskPriority.Medium,
            Source = TaskSource.DCS,
            ExternalId = record.DocumentId,
            AssigneeId = assignee.Id,
            CreatedById = actor.Id,
            OrganizationId = assignee.OrganizationId,
            DepartmentId = assignee.DepartmentId!.Value,
        };
        db.WorkTasks.Add(task);

        var channel = new MarketingRfqChannelRequest
        {
            Id = Guid.NewGuid(),
            MarketingRecordId = record.Id,
            DocumentId = record.DocumentId,
            Channel = MarketingRfqChannelType.Tenderweek,
            Status = MarketingRfqChannelStatus.Open,
            WorkTaskId = task.Id,
            AssignedUserId = assignee.Id,
            ExternalNumber = task.Number,
            CreatedById = actor.Id,
        };
        db.MarketingRfqChannelRequests.Add(channel);
        record.RfqChannelRequests.Add(channel);
        record.UpdatedAt = DateTime.UtcNow;

        return channel;
    }

    private async Task CompleteChannelAsync(MarketingRfqChannelRequest channel, CancellationToken ct)
    {
        channel.Status = MarketingRfqChannelStatus.Completed;
        channel.CompletedAt = DateTime.UtcNow;
        var record = channel.Record;
        record.UpdatedAt = DateTime.UtcNow;

        if (channel.Channel == MarketingRfqChannelType.AtgWebsite)
            record.RfqPublishedAtgSite = true;
        else
            record.RfqPublishedTenderweek = true;

        var dispatch = new RfqDispatch
        {
            Id = Guid.NewGuid(),
            MarketingRecordId = record.Id,
            DispatchType = channel.Channel == MarketingRfqChannelType.AtgWebsite
                ? RfqDispatchType.AtgSite
                : RfqDispatchType.Tenderweek,
            RecipientName = channel.Channel == MarketingRfqChannelType.AtgWebsite
                ? "HO IT Digitalization"
                : channel.AssignedUserId is not null
                    ? await db.Users.Where(u => u.Id == channel.AssignedUserId).Select(u => u.FullName).FirstOrDefaultAsync(ct)
                    : "Tender Section",
            Notes = $"Completed via {channel.ExternalNumber}",
            ResponseReceivedAt = DateTime.UtcNow,
        };
        db.RfqDispatches.Add(dispatch);
        record.RfqDispatches.Add(dispatch);
        record.Status = MarketingRecordStatus.RfqSent;

        await db.SaveChangesAsync(ct);
    }

    private static void EnsureStep4(MarketingRecord record)
    {
        if (record.Request?.MarketingCurrentStep != 4)
            throw new InvalidOperationException("RFQ channel requests are only available at marketing step 4");
    }

    private static void EnsureRfqDocument(MarketingRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.RfqDocumentStorageKey))
            throw new InvalidOperationException("Upload the RFQ document first");
    }

    private static string BuildAtgWebsiteDescription(MarketingRecord record, string docNumber)
    {
        var file = record.RfqDocumentFileName ?? "RFQ document";
        return $"Procurement request {docNumber} requires RFQ publication on the ATG official website.\n\n"
            + $"RFQ file: {file}\n"
            + $"Document ID: {record.DocumentId}\n\n"
            + "Please publish the RFQ on the website and mark the Help Desk ticket Done. "
            + "The marketing specialist will close the ticket to complete the channel.";
    }

    private static string BuildTenderDescription(MarketingRecord record, string docNumber)
    {
        var file = record.RfqDocumentFileName ?? "RFQ document";
        return $"Procurement request {docNumber} requires RFQ publication on tenderweek.com.\n\n"
            + $"RFQ file: {file}\n"
            + $"Document ID: {record.DocumentId}";
    }

    private async Task<User?> ResolveTenderOfficerAsync(CancellationToken ct)
    {
        var tndDept = await db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == HoMktTnd, ct);
        if (tndDept is null) return null;

        var officers = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.DepartmentId == tndDept.Id
                && TenderOfficerEmails.Contains(u.Email))
            .ToListAsync(ct);
        if (officers.Count == 0)
        {
            return await db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.DepartmentId == tndDept.Id)
                .OrderBy(u => u.LastName)
                .FirstOrDefaultAsync(ct);
        }

        var openCounts = await db.MarketingRfqChannelRequests.AsNoTracking()
            .Where(c => c.Channel == MarketingRfqChannelType.Tenderweek
                && c.Status == MarketingRfqChannelStatus.Open
                && c.AssignedUserId != null)
            .GroupBy(c => c.AssignedUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return officers
            .OrderBy(o => openCounts.FirstOrDefault(c => c.UserId == o.Id)?.Count ?? 0)
            .ThenBy(o => o.LastName)
            .First();
    }

    private async Task<string> GenerateTicketNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"HD-{year}-";
        var last = await db.Tickets
            .Where(t => t.Number.StartsWith(prefix))
            .OrderByDescending(t => t.Number)
            .Select(t => t.Number)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;
        return $"{prefix}{seq:D5}";
    }

    private async Task<string> GenerateTaskNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"TSK-{year}-";
        var last = await db.WorkTasks
            .Where(t => t.Number.StartsWith(prefix))
            .OrderByDescending(t => t.Number)
            .Select(t => t.Number)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;
        return $"{prefix}{seq:D5}";
    }
}
