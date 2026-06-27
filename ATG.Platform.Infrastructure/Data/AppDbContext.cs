using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketActivity> TicketActivities => Set<TicketActivity>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentActivity> DocumentActivities => Set<DocumentActivity>();
    public DbSet<ProcurementRequestDetail> ProcurementRequestDetails => Set<ProcurementRequestDetail>();
    public DbSet<ProcurementRequestApprover> ProcurementRequestApprovers => Set<ProcurementRequestApprover>();
    public DbSet<ProcurementRequestAttachment> ProcurementRequestAttachments => Set<ProcurementRequestAttachment>();
    public DbSet<IncomingLetterDetail> IncomingLetterDetails => Set<IncomingLetterDetail>();
    public DbSet<IncomingLetterRecipient> IncomingLetterRecipients => Set<IncomingLetterRecipient>();
    public DbSet<IncomingLetterComment> IncomingLetterComments => Set<IncomingLetterComment>();
    public DbSet<MarketingRecord> MarketingRecords => Set<MarketingRecord>();
    public DbSet<MarketingOffer> MarketingOffers => Set<MarketingOffer>();
    public DbSet<RfqDispatch> RfqDispatches => Set<RfqDispatch>();
    public DbSet<MarketingProcurementPlan> MarketingProcurementPlans => Set<MarketingProcurementPlan>();
    public DbSet<MarketingPortalApproval> MarketingPortalApprovals => Set<MarketingPortalApproval>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(e =>
        {
            e.ToTable("organizations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Code).HasMaxLength(20);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.OrgType).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.NameEn).HasMaxLength(100);
            e.Property(x => x.Code).HasMaxLength(30);
            e.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            e.HasOne(x => x.Organization).WithMany(x => x.Departments).HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Position>(e =>
        {
            e.ToTable("positions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Code).HasMaxLength(30);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.EmployeeId).HasMaxLength(20);
            e.HasIndex(x => x.EmployeeId).IsUnique();
            e.Property(x => x.FirstName).HasMaxLength(50);
            e.Property(x => x.LastName).HasMaxLength(50);
            e.Property(x => x.MiddleName).HasMaxLength(50);
            e.Property(x => x.FirstNameEn).HasMaxLength(50);
            e.Property(x => x.LastNameEn).HasMaxLength(50);
            e.Property(x => x.MiddleNameEn).HasMaxLength(50);
            e.Property(x => x.JobTitleRu).HasMaxLength(150);
            e.Property(x => x.JobTitleEn).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Language).HasMaxLength(5);
            e.HasOne(x => x.Organization).WithMany(x => x.Users).HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.Department).WithMany(x => x.Users).HasForeignKey(x => x.DepartmentId);
            e.HasOne(x => x.Position).WithMany(x => x.Users).HasForeignKey(x => x.PositionId);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100);
            e.Property(x => x.EntityType).HasMaxLength(50);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.ToTable("tickets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Number).HasMaxLength(20);
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TargetDepartment).WithMany().HasForeignKey(x => x.TargetDepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AssignedBy).WithMany().HasForeignKey(x => x.AssignedById).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TicketComment>(e =>
        {
            e.ToTable("ticket_comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).HasMaxLength(4000);
            e.HasOne(x => x.Ticket).WithMany(x => x.Comments).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketActivity>(e =>
        {
            e.ToTable("ticket_activities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(50);
            e.Property(x => x.Details).HasMaxLength(500);
            e.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Ticket).WithMany(x => x.Activities).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkTask>(e =>
        {
            e.ToTable("work_tasks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Number).HasMaxLength(20);
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Number).HasMaxLength(30);
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ExternalReference).HasMaxLength(100);
            e.Property(x => x.TitleRu).HasMaxLength(200);
            e.Property(x => x.IncomingNumber).HasMaxLength(50);
            e.Property(x => x.RecordBook).HasMaxLength(100);
            e.Property(x => x.SenderName).HasMaxLength(200);
            e.Property(x => x.ReceiverName).HasMaxLength(150);
            e.Property(x => x.AttachmentFileName).HasMaxLength(255);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DocumentActivity>(e =>
        {
            e.ToTable("document_activities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(50);
            e.Property(x => x.Details).HasMaxLength(500);
            e.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Document).WithMany(x => x.Activities).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProcurementRequestDetail>(e =>
        {
            e.ToTable("procurement_request_details");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Flow).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.MarketingSubPhase).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.MarketingActiveBranch).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.MarketingCurrentStep).HasDefaultValue(1);
            e.Property(x => x.EamNumber).HasMaxLength(50);
            e.HasOne(x => x.Document).WithOne().HasForeignKey<ProcurementRequestDetail>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Initiator).WithMany().HasForeignKey(x => x.InitiatorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.InitiatorDepartment).WithMany().HasForeignKey(x => x.InitiatorDepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.MarketingSpecialist).WithMany().HasForeignKey(x => x.MarketingSpecialistId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.MarketingRecord).WithOne(x => x.Request).HasForeignKey<MarketingRecord>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MarketingRecord>(e =>
        {
            e.ToTable("marketing_records");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DocumentId).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.MarketingExecutorId);
            e.HasIndex(x => x.DeadlineDate);
            e.HasIndex(x => x.RequestCategory);
            e.Property(x => x.PortalNumber).HasMaxLength(50);
            e.HasIndex(x => x.PortalNumber).IsUnique();
            e.Property(x => x.InitiatorDepartment).HasMaxLength(255);
            e.Property(x => x.InitiatorFullName).HasMaxLength(255);
            e.Property(x => x.RequestTitle).HasMaxLength(500);
            e.Property(x => x.ProcurementMethod).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.StrategyNumber).HasMaxLength(100);
            e.Property(x => x.StrategyNumberManual).HasMaxLength(100);
            e.Property(x => x.BudgetCurrency).HasMaxLength(10);
            e.Property(x => x.PortalBudgetNumber).HasMaxLength(100);
            e.Property(x => x.PortalApprovalType).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.RequestCategory).HasConversion<int?>();
            e.Property(x => x.BudgetAmount).HasPrecision(15, 2);
            e.HasOne(x => x.MarketingExecutor).WithMany().HasForeignKey(x => x.MarketingExecutorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AssignedByManager).WithMany().HasForeignKey(x => x.AssignedByManagerId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MarketingOffer>(e =>
        {
            e.ToTable("marketing_offers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MarketingRecordId);
            e.Property(x => x.CompanyName).HasMaxLength(500);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.OfferAmount).HasPrecision(15, 2);
            e.HasOne(x => x.Record).WithMany(x => x.Offers).HasForeignKey(x => x.MarketingRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RfqDispatch>(e =>
        {
            e.ToTable("rfq_dispatches");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MarketingRecordId);
            e.Property(x => x.DispatchType).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.RecipientName).HasMaxLength(500);
            e.Property(x => x.RecipientEmail).HasMaxLength(255);
            e.Property(x => x.RecipientPhone).HasMaxLength(50);
            e.HasOne(x => x.Record).WithMany(x => x.RfqDispatches).HasForeignKey(x => x.MarketingRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MarketingProcurementPlan>(e =>
        {
            e.ToTable("marketing_procurement_plans");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MarketingRecordId);
            e.Property(x => x.ProcurementMethod).HasConversion<string>().HasMaxLength(100);
            e.Property(x => x.StartPriceCurrency).HasMaxLength(10);
            e.Property(x => x.Incoterms).HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.StartPrice).HasPrecision(15, 2);
            e.HasOne(x => x.Record).WithMany(x => x.Plans).HasForeignKey(x => x.MarketingRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MarketingPortalApproval>(e =>
        {
            e.ToTable("marketing_portal_approvals");
            e.HasKey(x => x.Id);
            e.Property(x => x.ApprovalType).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.BudgetNumber).HasMaxLength(100);
            e.HasOne(x => x.Record).WithMany(x => x.PortalApprovals).HasForeignKey(x => x.MarketingRecordId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ProcurementPlan).WithMany().HasForeignKey(x => x.ProcurementPlanId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProcurementRequestApprover>(e =>
        {
            e.ToTable("procurement_request_approvers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Comment).HasMaxLength(500);
            e.HasOne(x => x.Request).WithMany(x => x.Approvers).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProcurementRequestAttachment>(e =>
        {
            e.ToTable("procurement_request_attachments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.FileName).HasMaxLength(255);
            e.Property(x => x.StorageKey).HasMaxLength(500);
            e.HasOne(x => x.Request).WithMany(x => x.Attachments).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IncomingLetterDetail>(e =>
        {
            e.ToTable("incoming_letter_details");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.Document).WithOne().HasForeignKey<IncomingLetterDetail>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RoutedBy).WithMany().HasForeignKey(x => x.RoutedById).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.RoutedToDepartment).WithMany().HasForeignKey(x => x.RoutedToDepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<IncomingLetterRecipient>(e =>
        {
            e.ToTable("incoming_letter_recipients");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Letter).WithMany(x => x.Recipients).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IncomingLetterComment>(e =>
        {
            e.ToTable("incoming_letter_comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).HasMaxLength(4000);
            e.HasOne(x => x.Letter).WithMany(x => x.Comments).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
