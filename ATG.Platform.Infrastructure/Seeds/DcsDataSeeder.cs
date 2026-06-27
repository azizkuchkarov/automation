using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Dcs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATG.Platform.Infrastructure.Seeds;

public static class DcsDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Documents.AnyAsync())
            await SeedInitialDocumentsAsync(db);

        await EnsureIncomingLetterDemoAsync(db);
    }

    private static async Task SeedInitialDocumentsAsync(AppDbContext db)
    {
        var author = await db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == "f.asadov@atg.uz");
        if (author is null) return;

        var dept = await db.Departments.FirstOrDefaultAsync(d => d.Code == "HO-CPROC");
        if (dept is null) return;

        var samples = new[]
        {
            ("TA-2026-434", "Technical assignment — pipeline inspection system", DocumentType.TechnicalAssignment, DocumentStatus.Registered),
            ("TA-2026-374", "Technical assignment — SCADA upgrade phase 2", DocumentType.TechnicalAssignment, DocumentStatus.InReview),
            ("TA-2026-433", "Technical assignment — valve maintenance program", DocumentType.TechnicalAssignment, DocumentStatus.Approved),
            ("TA-2026-367", "Technical assignment — compressor station automation", DocumentType.TechnicalAssignment, DocumentStatus.Draft),
            ("OUT-2026-001", "Outgoing — Response to audit inquiry", DocumentType.Outgoing, DocumentStatus.InReview),
            ("MR-2026-001", "Material requisition — office supplies Q2", DocumentType.MaterialServiceRequisition, DocumentStatus.Registered),
            ("CON-2026-001", "Contract — IT infrastructure maintenance", DocumentType.Contract, DocumentStatus.InReview),
        };

        foreach (var (number, title, type, status) in samples)
        {
            var typeDeptCode = type switch
            {
                DocumentType.Outgoing => "HO-DCPR-CLER",
                DocumentType.MaterialServiceRequisition => "HO-CPROC-DOM",
                DocumentType.Contract => "HO-CPROC-CADM",
                _ => "HO-CPROC"
            };
            var docDept = await db.Departments.FirstOrDefaultAsync(d => d.Code == typeDeptCode) ?? dept;

            db.Documents.Add(new Document
            {
                Number = number,
                Title = title,
                Description = title,
                Type = type,
                Status = status,
                AuthorId = author.Id,
                OrganizationId = author.OrganizationId,
                DepartmentId = docDept.Id,
                RegisteredAt = status != DocumentStatus.Draft ? DateTime.UtcNow.AddDays(-7) : null,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureIncomingLetterDemoAsync(AppDbContext db)
    {
        const string regNum = "OTHER-LI-8712";
        var marina = await db.Users.FirstOrDefaultAsync(u => u.Email == DcsRouting.IncomingRegistrarEmail);

        var existing = await db.Documents.FirstOrDefaultAsync(d => d.Number == regNum);
        if (existing is not null)
        {
            if (marina is not null)
            {
                existing.AuthorId = marina.Id;
                existing.ReceiverName = "Azizov Mirjalol";
                existing.AssigneeId = null;
            }

            var hasDetail = await db.IncomingLetterDetails.AnyAsync(d => d.DocumentId == existing.Id);
            if (!hasDetail)
            {
                db.IncomingLetterDetails.Add(new IncomingLetterDetail
                {
                    DocumentId = existing.Id,
                    Phase = IncomingLetterPhase.Registered,
                });
            }

            await db.SaveChangesAsync();
            return;
        }

        if (marina is null) return;

        var dept = await db.Departments.FirstOrDefaultAsync(d => d.Code == "HO-DCPR-CLER");
        if (dept is null) return;

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Number = regNum,
            Title = "Guarantee Letter for CBM System",
            TitleRu = "Гарантийное письмо по системе CBM",
            Description = "Guarantee Letter for CBM System",
            Type = DocumentType.Incoming,
            Status = DocumentStatus.InReview,
            IncomingNumber = "№ NA",
            IncomingDate = new DateTime(2025, 7, 16, 0, 0, 0, DateTimeKind.Utc),
            RegisteredAt = new DateTime(2025, 7, 24, 0, 0, 0, DateTimeKind.Utc),
            SenderName = "Tianjin BaoDe Energy Technology Company Limited",
            ReceiverName = "Azizov Mirjalol",
            AttachmentFileName = "Guarantee_Letter_CBM.pdf",
            TranslationRequestCount = 0,
            AuthorId = marina.Id,
            OrganizationId = marina.OrganizationId,
            DepartmentId = dept.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        db.Documents.Add(doc);
        db.IncomingLetterDetails.Add(new IncomingLetterDetail
        {
            DocumentId = doc.Id,
            Phase = IncomingLetterPhase.Registered,
        });

        await db.SaveChangesAsync();
    }
}
