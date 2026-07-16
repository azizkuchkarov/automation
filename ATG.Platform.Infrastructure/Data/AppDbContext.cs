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
    public DbSet<ProcurementMarketingPlanApprover> ProcurementMarketingPlanApprovers => Set<ProcurementMarketingPlanApprover>();
    public DbSet<ProcurementRequestAttachment> ProcurementRequestAttachments => Set<ProcurementRequestAttachment>();
    public DbSet<ProcurementContractsIntStepFile> ProcurementContractsIntStepFiles => Set<ProcurementContractsIntStepFile>();
    public DbSet<ProcurementContractsIntStepApprover> ProcurementContractsIntStepApprovers => Set<ProcurementContractsIntStepApprover>();
    public DbSet<ProcurementContractsDomStepFile> ProcurementContractsDomStepFiles => Set<ProcurementContractsDomStepFile>();
    public DbSet<ProcurementContractsDomStepApprover> ProcurementContractsDomStepApprovers => Set<ProcurementContractsDomStepApprover>();
    public DbSet<ProcurementStepComment> ProcurementStepComments => Set<ProcurementStepComment>();
    public DbSet<ProcurementWorkflowRoleAssignment> ProcurementWorkflowRoleAssignments => Set<ProcurementWorkflowRoleAssignment>();
    public DbSet<ItAsset> ItAssets => Set<ItAsset>();
    public DbSet<ItAutomationRoleAssignment> ItAutomationRoleAssignments => Set<ItAutomationRoleAssignment>();
    public DbSet<IncomingLetterDetail> IncomingLetterDetails => Set<IncomingLetterDetail>();
    public DbSet<IncomingLetterRecipient> IncomingLetterRecipients => Set<IncomingLetterRecipient>();
    public DbSet<IncomingLetterComment> IncomingLetterComments => Set<IncomingLetterComment>();
    public DbSet<OutgoingLetterDetail> OutgoingLetterDetails => Set<OutgoingLetterDetail>();
    public DbSet<OutgoingLetterCoordinator> OutgoingLetterCoordinators => Set<OutgoingLetterCoordinator>();
    public DbSet<OutgoingLetterComment> OutgoingLetterComments => Set<OutgoingLetterComment>();
    public DbSet<MemoDetail> MemoDetails => Set<MemoDetail>();
    public DbSet<MemoRecipient> MemoRecipients => Set<MemoRecipient>();
    public DbSet<MemoCoordinator> MemoCoordinators => Set<MemoCoordinator>();
    public DbSet<MemoComment> MemoComments => Set<MemoComment>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<OrderCoordinator> OrderCoordinators => Set<OrderCoordinator>();
    public DbSet<OrderRecipient> OrderRecipients => Set<OrderRecipient>();
    public DbSet<OrderComment> OrderComments => Set<OrderComment>();
    public DbSet<MarketingRecord> MarketingRecords => Set<MarketingRecord>();
    public DbSet<MarketingOffer> MarketingOffers => Set<MarketingOffer>();
    public DbSet<RfqDispatch> RfqDispatches => Set<RfqDispatch>();
    public DbSet<MarketingRfqChannelRequest> MarketingRfqChannelRequests => Set<MarketingRfqChannelRequest>();
    public DbSet<MarketingProcurementPlan> MarketingProcurementPlans => Set<MarketingProcurementPlan>();
    public DbSet<MarketingPortalApproval> MarketingPortalApprovals => Set<MarketingPortalApproval>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<HrLeaveRequestDetail> HrLeaveRequestDetails => Set<HrLeaveRequestDetail>();
    public DbSet<HrLeaveRequestItem> HrLeaveRequestItems => Set<HrLeaveRequestItem>();
    public DbSet<HrLeaveApprover> HrLeaveApprovers => Set<HrLeaveApprover>();
    public DbSet<HrLeaveSignature> HrLeaveSignatures => Set<HrLeaveSignature>();
    public DbSet<HrBusinessTripRequestDetail> HrBusinessTripRequestDetails => Set<HrBusinessTripRequestDetail>();
    public DbSet<HrBusinessTripSignature> HrBusinessTripSignatures => Set<HrBusinessTripSignature>();
    public DbSet<HrBusinessTripTraveler> HrBusinessTripTravelers => Set<HrBusinessTripTraveler>();
    public DbSet<HrBusinessTripApprover> HrBusinessTripApprovers => Set<HrBusinessTripApprover>();
    public DbSet<HrBusinessTripDeptWorkflow> HrBusinessTripDeptWorkflows => Set<HrBusinessTripDeptWorkflow>();
    public DbSet<HrBusinessTripWorkflowTier> HrBusinessTripWorkflowTiers => Set<HrBusinessTripWorkflowTier>();
    public DbSet<HrBusinessTripWorkflowInitiator> HrBusinessTripWorkflowInitiators => Set<HrBusinessTripWorkflowInitiator>();
    public DbSet<HrBusinessTripWorkflowStep> HrBusinessTripWorkflowSteps => Set<HrBusinessTripWorkflowStep>();

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
            e.Property(x => x.NameGenitive).HasMaxLength(150);
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
            e.Property(x => x.Pinpp).HasMaxLength(14);
            e.Property(x => x.PassportSeries).HasMaxLength(10);
            e.Property(x => x.PassportNumber).HasMaxLength(20);
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
            e.Property(x => x.Title).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.SourceLanguage).HasMaxLength(30);
            e.Property(x => x.TranslatingLanguage).HasMaxLength(100);
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
            e.Property(x => x.Title).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.AssigneeId);
            e.HasIndex(x => new { x.OrganizationId, x.Status });
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Number).HasMaxLength(30);
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.Title).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Type);
            e.HasIndex(x => x.AuthorId);
            e.Property(x => x.ExternalReference).HasMaxLength(100);
            e.Property(x => x.TitleRu).HasMaxLength(500);
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

        modelBuilder.Entity<ProcurementWorkflowRoleAssignment>(e =>
        {
            e.ToTable("procurement_workflow_role_assignments");
            e.HasKey(x => x.RoleKey);
            e.Property(x => x.RoleKey).HasConversion<string>().HasMaxLength(40);
            e.HasOne(x => x.ManagerUser).WithMany().HasForeignKey(x => x.ManagerUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.EngineerDepartment).WithMany().HasForeignKey(x => x.EngineerDepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ItAsset>(e =>
        {
            e.ToTable("it_assets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.NameRu).HasMaxLength(500);
            e.Property(x => x.NameEn).HasMaxLength(500);
            e.Property(x => x.Quantity).HasMaxLength(100);
            e.Property(x => x.Term).HasMaxLength(100);
            e.Property(x => x.BudgetCode).HasMaxLength(50);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.ContractNumber).HasMaxLength(120);
            e.Property(x => x.Note).HasMaxLength(2000);
            e.Property(x => x.BudgetAmount).HasPrecision(18, 2);
            e.Property(x => x.Cost).HasPrecision(18, 2);
            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.ExpiresAt);
            e.HasIndex(x => x.PlanYear);
            e.HasOne(x => x.ResponsibleUser).WithMany().HasForeignKey(x => x.ResponsibleUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ItAutomationRoleAssignment>(e =>
        {
            e.ToTable("it_automation_role_assignments");
            e.HasKey(x => x.Category);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
            e.HasOne(x => x.ResponsibleUser).WithMany().HasForeignKey(x => x.ResponsibleUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProcurementRequestDetail>(e =>
        {
            e.ToTable("procurement_request_details");

            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Flow).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.MarketingSubPhase).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.ContractsSubPhase).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.ContractsProcurementSection).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.ContractsIntVariant).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.ContractsIntCurrentStep).HasDefaultValue(0);
            e.Property(x => x.ContractsIntContractRegistrationNumber).HasMaxLength(50);
            e.Property(x => x.ContractsDomVariant).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.ContractsDomCurrentStep).HasDefaultValue(0);
            e.Property(x => x.ContractsDomContractRegistrationNumber).HasMaxLength(50);
            e.Property(x => x.ContractsDomPriceRequestDate);
            e.Property(x => x.ContractsDomPriceResponseDueDate);
            e.Property(x => x.ContractsDomDeliveryDueDate);
            e.Property(x => x.ContractsDomActualDeliveryDate);
            e.Property(x => x.ContractsDomLastTerminationAt);
            e.Property(x => x.PaymentSubPhase).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.MarketingActiveBranch).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.MarketingCurrentStep).HasDefaultValue(1);
            e.Property(x => x.EamNumber).HasMaxLength(50);
            e.Property(x => x.TasRequisitionType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Region).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RegionLabelRu).HasMaxLength(150);
            e.Property(x => x.RegionLabelEn).HasMaxLength(150);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Document).WithOne().HasForeignKey<ProcurementRequestDetail>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Initiator).WithMany().HasForeignKey(x => x.InitiatorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.TasResponsible).WithMany().HasForeignKey(x => x.TasResponsibleId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.InitiatorDepartment).WithMany().HasForeignKey(x => x.InitiatorDepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.MarketingSpecialist).WithMany().HasForeignKey(x => x.MarketingSpecialistId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ContractsSpecialist).WithMany().HasForeignKey(x => x.ContractsSpecialistId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ContractsIntSecretariatUser).WithMany().HasForeignKey(x => x.ContractsIntSecretariatUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ContractsDomContractsAdminUser).WithMany().HasForeignKey(x => x.ContractsDomContractsAdminUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PaymentSpecialist).WithMany().HasForeignKey(x => x.PaymentSpecialistId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.MarketingPlanRegistrationNumber).HasMaxLength(50);
            e.HasOne(x => x.MarketingRecord).WithOne(x => x.Request).HasForeignKey<MarketingRecord>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Phase);
            e.HasIndex(x => x.MarketingSpecialistId);
            e.HasIndex(x => x.ContractsSpecialistId);
            e.HasIndex(x => x.PaymentSpecialistId);
        });

        modelBuilder.Entity<ProcurementContractsIntStepFile>(e =>
        {
            e.ToTable("procurement_contracts_int_step_files");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.DocumentId, x.StepNumber });
            e.Property(x => x.FileName).HasMaxLength(260);
            e.Property(x => x.StorageKey).HasMaxLength(500);
            e.HasOne(x => x.Request).WithMany(x => x.ContractsIntStepFiles).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProcurementContractsIntStepApprover>(e =>
        {
            e.ToTable("procurement_contracts_int_step_approvers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.DocumentId, x.StepNumber });
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Comment).HasMaxLength(1000);
            e.HasOne(x => x.Request).WithMany(x => x.ContractsIntStepApprovers).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProcurementContractsDomStepFile>(e =>
        {
            e.ToTable("procurement_contracts_dom_step_files");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.DocumentId, x.StepNumber });
            e.Property(x => x.FileName).HasMaxLength(260);
            e.Property(x => x.StorageKey).HasMaxLength(500);
            e.HasOne(x => x.Request).WithMany(x => x.ContractsDomStepFiles).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProcurementContractsDomStepApprover>(e =>
        {
            e.ToTable("procurement_contracts_dom_step_approvers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.DocumentId, x.StepNumber });
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Comment).HasMaxLength(1000);
            e.HasOne(x => x.Request).WithMany(x => x.ContractsDomStepApprovers).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProcurementMarketingPlanApprover>(e =>
        {
            e.ToTable("procurement_marketing_plan_approvers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DocumentId);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Request).WithMany(x => x.MarketingPlanApprovers).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
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
            e.Property(x => x.RfqDocumentStorageKey).HasMaxLength(500);
            e.Property(x => x.RfqDocumentFileName).HasMaxLength(255);
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

        modelBuilder.Entity<MarketingRfqChannelRequest>(e =>
        {
            e.ToTable("marketing_rfq_channel_requests");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MarketingRecordId);
            e.HasIndex(x => x.DocumentId);
            e.HasIndex(x => x.HelpDeskTicketId);
            e.HasIndex(x => x.WorkTaskId);
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ExternalNumber).HasMaxLength(50);
            e.HasOne(x => x.Record).WithMany(x => x.RfqChannelRequests).HasForeignKey(x => x.MarketingRecordId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MarketingProcurementPlan>(e =>
        {
            e.ToTable("marketing_procurement_plans");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MarketingRecordId);
            e.Property(x => x.ProcurementMethod).HasConversion<string>().HasMaxLength(100);
            e.Property(x => x.RegistrationMethod).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.RegistrationNumber).HasMaxLength(50);
            e.Property(x => x.TemplateStorageKey).HasMaxLength(500);
            e.Property(x => x.TemplateFileName).HasMaxLength(255);
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

        modelBuilder.Entity<ProcurementStepComment>(e =>
        {
            e.ToTable("procurement_step_comments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.DocumentId, x.Phase, x.StepNumber });
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Body).HasMaxLength(4000);
            e.HasOne(x => x.Request).WithMany(x => x.StepComments).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IncomingLetterDetail>(e =>
        {
            e.ToTable("incoming_letter_details");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.Document).WithOne().HasForeignKey<IncomingLetterDetail>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RoutedBy).WithMany().HasForeignKey(x => x.RoutedById).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.RoutedToDepartment).WithMany().HasForeignKey(x => x.RoutedToDepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ResolutionManager).WithMany().HasForeignKey(x => x.ResolutionManagerId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.AssignmentTask).HasMaxLength(2000);
            e.Property(x => x.SourceLanguage).HasMaxLength(30);
            e.Property(x => x.TranslatingLanguage).HasMaxLength(100);
            e.Property(x => x.TranslatedAttachmentFileName).HasMaxLength(255);
            e.Property(x => x.TranslatedAttachmentStorageKey).HasMaxLength(500);
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

        modelBuilder.Entity<OutgoingLetterDetail>(e =>
        {
            e.ToTable("outgoing_letter_details");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(40);
            e.HasOne(x => x.Document).WithOne().HasForeignKey<OutgoingLetterDetail>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DeptHead).WithMany().HasForeignKey(x => x.DeptHeadId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SupervisingDeputy).WithMany().HasForeignKey(x => x.SupervisingDeputyId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.FirstDeputy).WithMany().HasForeignKey(x => x.FirstDeputyId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.GeneralDirector).WithMany().HasForeignKey(x => x.GeneralDirectorId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.SourceLanguage).HasMaxLength(30);
            e.Property(x => x.TranslatingLanguage).HasMaxLength(100);
            e.Property(x => x.TranslatedAttachmentFileName).HasMaxLength(255);
            e.Property(x => x.TranslatedAttachmentStorageKey).HasMaxLength(500);
            e.Property(x => x.RevisionNotes).HasMaxLength(2000);
        });

        modelBuilder.Entity<OutgoingLetterCoordinator>(e =>
        {
            e.ToTable("outgoing_letter_coordinators");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Letter).WithMany(x => x.Coordinators).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OutgoingLetterComment>(e =>
        {
            e.ToTable("outgoing_letter_comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).HasMaxLength(4000);
            e.HasOne(x => x.Letter).WithMany(x => x.Comments).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MemoDetail>(e =>
        {
            e.ToTable("memo_details");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(40);
            e.HasOne(x => x.Document).WithOne().HasForeignKey<MemoDetail>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DeptHead).WithMany().HasForeignKey(x => x.DeptHeadId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ResolutionManager).WithMany().HasForeignKey(x => x.ResolutionManagerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.RoutedBy).WithMany().HasForeignKey(x => x.RoutedById).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.RoutedToDepartment).WithMany().HasForeignKey(x => x.RoutedToDepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.SourceLanguage).HasMaxLength(30);
            e.Property(x => x.TranslatingLanguage).HasMaxLength(100);
            e.Property(x => x.TranslatedAttachmentFileName).HasMaxLength(255);
            e.Property(x => x.TranslatedAttachmentStorageKey).HasMaxLength(500);
            e.Property(x => x.AssignmentTask).HasMaxLength(2000);
            e.Property(x => x.RevisionNotes).HasMaxLength(2000);
        });

        modelBuilder.Entity<MemoRecipient>(e =>
        {
            e.ToTable("memo_recipients");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Memo).WithMany(x => x.Recipients).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MemoCoordinator>(e =>
        {
            e.ToTable("memo_coordinators");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Memo).WithMany(x => x.Coordinators).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MemoComment>(e =>
        {
            e.ToTable("memo_comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).HasMaxLength(4000);
            e.HasOne(x => x.Memo).WithMany(x => x.Comments).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderDetail>(e =>
        {
            e.ToTable("order_details");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(40);
            e.HasOne(x => x.Document).WithOne().HasForeignKey<OrderDetail>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DeptHead).WithMany().HasForeignKey(x => x.DeptHeadId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.LegalHead).WithMany().HasForeignKey(x => x.LegalHeadId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SupervisingDeputy).WithMany().HasForeignKey(x => x.SupervisingDeputyId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.FirstDeputy).WithMany().HasForeignKey(x => x.FirstDeputyId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.GeneralDirector).WithMany().HasForeignKey(x => x.GeneralDirectorId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.RevisionNotes).HasMaxLength(2000);
            e.Property(x => x.ScanAttachmentFileName).HasMaxLength(255);
            e.Property(x => x.ScanAttachmentStorageKey).HasMaxLength(500);
        });

        modelBuilder.Entity<OrderCoordinator>(e =>
        {
            e.ToTable("order_coordinators");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Order).WithMany(x => x.Coordinators).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderRecipient>(e =>
        {
            e.ToTable("order_recipients");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Order).WithMany(x => x.Recipients).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderComment>(e =>
        {
            e.ToTable("order_comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).HasMaxLength(4000);
            e.HasOne(x => x.Order).WithMany(x => x.Comments).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HrLeaveRequestDetail>(e =>
        {
            e.ToTable("hr_leave_request_details");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Track).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.PeriodLabel).HasMaxLength(20);
            e.Property(x => x.SigningPayloadHash).HasMaxLength(64);
            e.Property(x => x.PdfStorageKey).HasMaxLength(500);
            e.Property(x => x.PdfSignedStorageKey).HasMaxLength(500);
            e.Property(x => x.PdfPresentationStorageKey).HasMaxLength(500);
            e.HasOne(x => x.Document).WithOne().HasForeignKey<HrLeaveRequestDetail>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.HrDepartment).WithMany().HasForeignKey(x => x.HrDepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HrLeaveRequestItem>(e =>
        {
            e.ToTable("hr_leave_request_items");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DocumentId);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.NoteRu).HasMaxLength(1000);
            e.Property(x => x.NoteEn).HasMaxLength(1000);
            e.HasOne(x => x.Request).WithMany(x => x.Items).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HrLeaveApprover>(e =>
        {
            e.ToTable("hr_leave_approvers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DocumentId);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Comment).HasMaxLength(2000);
            e.HasOne(x => x.Request).WithMany(x => x.Approvers).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HrLeaveSignature>(e =>
        {
            e.ToTable("hr_leave_signatures");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DocumentId);
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ApproverRole).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Pkcs7Base64).HasColumnType("text");
            e.Property(x => x.PayloadSha256).HasMaxLength(64);
            e.Property(x => x.CertificateSerial).HasMaxLength(100);
            e.Property(x => x.SignerPinpp).HasMaxLength(20);
            e.Property(x => x.SignerCn).HasMaxLength(300);
            e.Property(x => x.SignerTin).HasMaxLength(20);
            e.Property(x => x.StorageKey).HasMaxLength(500);
            e.HasOne(x => x.Request).WithMany(x => x.Signatures).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Signer).WithMany().HasForeignKey(x => x.SignerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HrBusinessTripRequestDetail>(e =>
        {
            e.ToTable("hr_business_trip_request_details");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Phase).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.PurposeRu).HasMaxLength(2000);
            e.Property(x => x.PurposeEn).HasMaxLength(2000);
            e.Property(x => x.PlaceRu).HasMaxLength(500);
            e.Property(x => x.PlaceEn).HasMaxLength(500);
            e.Property(x => x.PdfStorageKey).HasMaxLength(500);
            e.Property(x => x.PdfSignedStorageKey).HasMaxLength(500);
            e.Property(x => x.PdfPresentationStorageKey).HasMaxLength(500);
            e.Property(x => x.SigningPayloadHash).HasMaxLength(64);
            e.Property(x => x.OrderNumber).HasMaxLength(50);
            e.Property(x => x.OrderDocxStorageKey).HasMaxLength(500);
            e.HasOne(x => x.Document).WithOne().HasForeignKey<HrBusinessTripRequestDetail>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HrBusinessTripSignature>(e =>
        {
            e.ToTable("hr_business_trip_signatures");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DocumentId);
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ApproverRole).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Pkcs7Base64).HasColumnType("text");
            e.Property(x => x.PayloadSha256).HasMaxLength(64);
            e.Property(x => x.CertificateSerial).HasMaxLength(100);
            e.Property(x => x.SignerPinpp).HasMaxLength(20);
            e.Property(x => x.SignerCn).HasMaxLength(300);
            e.Property(x => x.SignerTin).HasMaxLength(20);
            e.Property(x => x.StorageKey).HasMaxLength(500);
            e.HasOne(x => x.Request).WithMany(x => x.Signatures).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Signer).WithMany().HasForeignKey(x => x.SignerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HrBusinessTripTraveler>(e =>
        {
            e.ToTable("hr_business_trip_travelers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DocumentId);
            e.Property(x => x.FullNameRu).HasMaxLength(200);
            e.Property(x => x.FullNameEn).HasMaxLength(200);
            e.Property(x => x.PositionRu).HasMaxLength(300);
            e.Property(x => x.PositionEn).HasMaxLength(300);
            e.Property(x => x.CertificateNumber).HasMaxLength(50);
            e.Property(x => x.CertificateStorageKey).HasMaxLength(500);
            e.HasOne(x => x.Request).WithMany(x => x.Travelers).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HrBusinessTripApprover>(e =>
        {
            e.ToTable("hr_business_trip_approvers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DocumentId);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Comment).HasMaxLength(2000);
            e.HasOne(x => x.Request).WithMany(x => x.Approvers).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HrBusinessTripDeptWorkflow>(e =>
        {
            e.ToTable("hr_business_trip_dept_workflows");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.DepartmentCode }).IsUnique();
            e.Property(x => x.DepartmentCode).HasMaxLength(40);
            e.Property(x => x.TitleRu).HasMaxLength(300);
            e.Property(x => x.TitleEn).HasMaxLength(300);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HrBusinessTripWorkflowTier>(e =>
        {
            e.ToTable("hr_business_trip_workflow_tiers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.WorkflowId, x.TierKey }).IsUnique();
            e.Property(x => x.TierKey).HasMaxLength(40);
            e.Property(x => x.TitleRu).HasMaxLength(300);
            e.Property(x => x.TitleEn).HasMaxLength(300);
            e.HasOne(x => x.Workflow).WithMany(x => x.Tiers).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HrBusinessTripWorkflowInitiator>(e =>
        {
            e.ToTable("hr_business_trip_workflow_initiators");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TierId, x.UserId }).IsUnique();
            e.HasOne(x => x.Tier).WithMany(x => x.Initiators).HasForeignKey(x => x.TierId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HrBusinessTripWorkflowStep>(e =>
        {
            e.ToTable("hr_business_trip_workflow_steps");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TierId, x.SortOrder }).IsUnique();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.LabelRu).HasMaxLength(300);
            e.Property(x => x.LabelEn).HasMaxLength(300);
            e.HasOne(x => x.Tier).WithMany(x => x.Steps).HasForeignKey(x => x.TierId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ApproverUser).WithMany().HasForeignKey(x => x.ApproverUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserNotification>(e =>
        {
            e.ToTable("user_notifications");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.Body).HasMaxLength(1000);
            e.Property(x => x.EntityType).HasMaxLength(40);
            e.Property(x => x.ActionUrl).HasMaxLength(500);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
