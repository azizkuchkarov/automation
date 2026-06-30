using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<Result<(string AccessToken, string RefreshToken)>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IUserService
{
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(int page, int pageSize, string? search, Guid? orgId, UserRole? role, bool? isActive, CancellationToken ct = default);
    Task<Result<UserDto>> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<bool>> DeactivateUserAsync(Guid id, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<bool>> ActivateUserAsync(Guid id, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<bool>> ResetPasswordAsync(Guid id, string newPassword, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<bool>> IsEmailUniqueAsync(string email, Guid? excludeId, CancellationToken ct = default);
    Task<Result<bool>> IsEmployeeIdUniqueAsync(string employeeId, Guid? excludeId, CancellationToken ct = default);
    Task<byte[]> ExportUsersCsvAsync(CancellationToken ct = default);
    Task<Result<ImportUsersResult>> ImportUsersAsync(ImportUsersRequest request, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<string>> GetNextEmployeeIdAsync(CancellationToken ct = default);
}

public interface IOrganizationService
{
    Task<Result<IReadOnlyList<OrganizationDto>>> GetTreeAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<OrgHierarchyDto>>> GetHierarchyAsync(CancellationToken ct = default);
    Task<Result<OrganizationDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<OrganizationDto>> CreateAsync(string name, string code, Guid? parentId, OrgType orgType, CancellationToken ct = default);
    Task<Result<OrganizationDto>> UpdateAsync(Guid id, string name, string code, CancellationToken ct = default);
}

public interface IDepartmentService
{
    Task<Result<IReadOnlyList<DepartmentDto>>> GetAllAsync(Guid? orgId, CancellationToken ct = default);
    Task<Result<DepartmentDto>> CreateAsync(Guid orgId, string name, string nameEn, string code, CancellationToken ct = default);
    Task<Result<DepartmentDto>> UpdateAsync(Guid id, string name, string nameEn, string code, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IPositionService
{
    Task<Result<IReadOnlyList<PositionDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PositionDto>> CreateAsync(string name, string code, CancellationToken ct = default);
    Task<Result<PositionDto>> UpdateAsync(Guid id, string name, string code, CancellationToken ct = default);
}

public interface IAuditService
{
    Task LogAsync(Guid? userId, string action, string? entityType, Guid? entityId, string? details, string? ipAddress, CancellationToken ct = default);
    Task<Result<PagedResult<AuditLogDto>>> GetLogsAsync(int page, int pageSize, Guid? userId, string? action, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(CancellationToken ct = default);
}

public interface IHelpDeskService
{
    IReadOnlyList<HelpDeskCategoryDto> GetCategories();
    Task<Result<PagedResult<TicketListItemDto>>> GetTicketsAsync(Guid actorId, string view, int page, int pageSize, TicketCategory? category, TicketStatus? status, CancellationToken ct = default);
    Task<Result<TicketBoardDto>> GetBoardAsync(Guid actorId, TicketCategory? category, CancellationToken ct = default);
    Task<Result<TicketDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<TicketDto>> CreateAsync(CreateTicketRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<TicketDto>> AssignAsync(Guid id, AssignTicketRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<TicketDto>> AcceptAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<TicketDto>> StartAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<TicketDto>> CompleteAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<TicketDto>> CloseAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<TicketCommentDto>> AddCommentAsync(Guid id, AddTicketCommentRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HelpDeskAssigneeDto>>> GetAssigneesAsync(Guid ticketId, Guid actorId, CancellationToken ct = default);
    Task<Result<HelpDeskDashboardDto>> GetAdminDashboardAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<HelpDeskAdminControlDto>> GetAdminControlAsync(Guid actorId, CancellationToken ct = default);
}

public interface IDcsService
{
    IReadOnlyList<DcsTypeDto> GetTypes();
    Task<Result<PagedResult<DocumentListItemDto>>> GetDocumentsAsync(
        Guid actorId, DocumentType type, string view, int page, int pageSize,
        DocumentStatus? status, CancellationToken ct = default);
    Task<Result<DocumentDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<DocumentDto>> CreateAsync(CreateDocumentRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<DocumentDto>> UpdateStatusAsync(Guid id, UpdateDocumentStatusRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<DcsDashboardDto>> GetAdminDashboardAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<DcsAdminControlDto>> GetAdminControlAsync(Guid actorId, CancellationToken ct = default);
}

public interface IProcurementRequestService
{
    IReadOnlyList<ProcurementStepDto> GetSteps();
    Task<Result<ProcurementCreateOptionsDto>> GetCreateOptionsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementInitiatorDepartmentDto>>> GetInitiatorDepartmentsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetInitiatorsAsync(Guid actorId, Guid departmentId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetResponsibleUsersAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> CreateTasAsync(CreateTasProcurementRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> CreateExpressAsync(CreateExpressProcurementRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> CompleteStepAsync(Guid id, int step, CompleteProcurementStepRequest? request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> SubmitStep9Async(Guid id, SubmitStep9Request request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ApproveAsync(Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> RejectAsync(Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ForwardToContractsAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetContractsWorkersAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AssignContractsSpecialistAsync(Guid id, AssignContractsSpecialistRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AcceptContractsAsync(Guid id, AcceptContractsRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> SubmitMarketingPlanApprovalAsync(Guid id, SubmitMarketingPlanApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ApproveMarketingPlanAsync(Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> RejectMarketingPlanAsync(Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ConfirmMarketingRegistrationAsync(Guid id, ConfirmMarketingRegistrationRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetMarketingPlanApproverUsersAsync(Guid actorId, string? search, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementMarketingQueueItemDto>>> GetMarketingQueueAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetMarketingWorkersAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AcceptMarketingAsync(Guid id, AcceptMarketingRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AssignMarketingSpecialistAsync(Guid id, AssignMarketingSpecialistRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AddStepCommentAsync(Guid id, AddProcurementStepCommentRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> CompleteMarketingAsync(Guid id, MarketingActionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    IReadOnlyList<ProcurementMarketingStepDto> GetMarketingSteps();
    Task<Result<ProcurementRequestDto>> CompleteMarketingStepAsync(Guid id, int step, CompleteMarketingStepRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> RecordMarketingBranchAsync(Guid id, MarketingBranchRequest request, Guid actorId, string? ip, CancellationToken ct = default);
}

public interface IMarketingService
{
    Task<Result<MarketingRecordDto>> CreateFromProcurementAsync(Guid documentId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> GetByDocumentIdAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MarketingRecordListItemDto>>> GetRecordsAsync(Guid actorId, MarketingRecordStatus? status, Guid? executorId, MarketingRequestCategory? category, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> SetCategoryAsync(Guid documentId, SetMarketingCategoryRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> AssignExecutorAsync(Guid documentId, AssignMarketingExecutorRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> AcceptAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> ReportTzIssueAsync(Guid documentId, MarketingTzIssueRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> ResolveTzIssueAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> MarkRfqPreparedAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> UploadRfqDocumentAsync(Guid documentId, UploadRfqDocumentRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> OpenRfqAtgWebsiteChannelAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> OpenRfqTenderChannelAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> AddRfqDispatchAsync(Guid documentId, AddRfqDispatchRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> MarkFollowupSentAsync(Guid dispatchId, MarkRfqFollowupRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingOfferDto>> AddOfferAsync(Guid documentId, AddMarketingOfferRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingOfferDto>> UpdateOfferComplianceAsync(Guid offerId, UpdateOfferComplianceRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingOfferDto>> UpdateOfferAffiliationAsync(Guid offerId, UpdateOfferAffiliationRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingProcurementPlanDto>> CreateProcurementPlanAsync(Guid documentId, CreateMarketingPlanRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingProcurementPlanDto>> SubmitPlanToManagementAsync(Guid planId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingProcurementPlanDto>> RejectPlanByManagementAsync(Guid planId, string notes, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> SubmitToPortalAsync(Guid documentId, SubmitPortalRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> CompletePortalApprovalAsync(Guid portalApprovalId, CompletePortalApprovalRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> CompleteToContractAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> CancelAsync(Guid documentId, MarketingCancelRequest request, Guid actorId, CancellationToken ct = default);
    Task SyncStatusFromWorkflowAsync(Guid documentId, CancellationToken ct = default);
    Task<Result<MarketingStatsDto>> GetStatsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MarketingBoardColumnDto>>> GetBoardAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MarketingLeadershipRowDto>>> GetLeadershipOverviewAsync(Guid actorId, CancellationToken ct = default);
    Task ProcessPortalApprovalRemindersAsync(CancellationToken ct = default);
    Task ProcessDeadlineWarningsAsync(CancellationToken ct = default);
}

public interface IIncomingLetterService
{
    Task<Result<IncomingLetterPermissionsDto>> GetPermissionsAsync(Guid actorId, Guid? documentId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<IncomingLetterUserDto>>> GetTopManagersAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<IncomingLetterDepartmentDto>>> GetDepartmentsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<IncomingLetterUserDto>>> GetDepartmentWorkersAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> CreateAsync(CreateIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> InformTopManagersAsync(Guid id, InformTopManagersRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> RouteToDepartmentAsync(Guid id, RouteIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> AssignWorkerAsync(Guid id, AssignIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> CompleteAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterCommentDto>> AddCommentAsync(Guid id, IncomingLetterCommentRequest request, Guid actorId, CancellationToken ct = default);
}

public interface ITaskService
{
    Task<Result<TaskNavigationDto>> GetNavigationAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<TaskAnalyticsDto>> GetAnalyticsAsync(Guid actorId, Guid? organizationId, Guid? departmentId, CancellationToken ct = default);
    Task<Result<PagedResult<TaskListItemDto>>> GetTasksAsync(Guid actorId, string view, int page, int pageSize, WorkTaskStatus? status, TaskSource? source, Guid? organizationId, Guid? departmentId, CancellationToken ct = default);
    Task<Result<TaskListItemDto>> CreateAsync(CreateTaskRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<TaskListItemDto>> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserDto>>> GetAssignableUsersAsync(Guid actorId, CancellationToken ct = default);
}

public interface IJwtService
{
    string GenerateAccessToken(User user);
}

public interface INotificationService
{
    Task NotifyDcsApprovalRequiredAsync(Guid recipientId, string documentNumber, string documentTitle, Guid documentId, CancellationToken ct = default);
    Task NotifyMarketingPlanApprovalRequiredAsync(Guid recipientId, string documentNumber, Guid documentId, CancellationToken ct = default);
    Task NotifyTaskAssignedAsync(Guid recipientId, string taskNumber, string taskTitle, Guid taskId, TaskSource source, Guid? externalId, CancellationToken ct = default);
    Task NotifyTicketAssignedAsync(Guid recipientId, string ticketNumber, string ticketTitle, Guid ticketId, CancellationToken ct = default);
    Task NotifyDcsApprovalRejectedAsync(Guid recipientId, string documentNumber, Guid documentId, CancellationToken ct = default);
    Task ProcessApprovalRemindersAsync(CancellationToken ct = default);
    Task<Result<PagedResult<NotificationDto>>> GetInboxAsync(Guid actorId, bool unreadOnly, int page, int pageSize, CancellationToken ct = default);
    Task<Result<NotificationUnreadCountDto>> GetUnreadCountAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<bool>> MarkReadAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<bool>> MarkAllReadAsync(Guid actorId, CancellationToken ct = default);
}

public interface ILdapService
{
    /// <summary>Authenticates against LDAP/AD. Returns normalized email on success.</summary>
    Task<Result<string>> AuthenticateAsync(string login, string password, CancellationToken ct = default);
}
