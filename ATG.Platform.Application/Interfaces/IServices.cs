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
    Task<Result<UserDto>> SetMyPinppAsync(Guid userId, string pinpp, string? ipAddress, CancellationToken ct = default);
    Task<Result<UserDto>> CompleteMyProfileAsync(
        Guid userId, CompleteMyProfileRequest request, string? ipAddress, CancellationToken ct = default);
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
    Task<Result<TicketDto>> UploadTranslationDocumentAsync(Guid id, UploadTranslationDocumentRequest request, Guid actorId, string? ip, CancellationToken ct = default);
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
    Task<Result<ProcurementRequestDto>> RejectTasAsync(Guid id, CompleteProcurementStepRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ApproveAsync(Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> RejectAsync(Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ForwardToContractsAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetContractsWorkersAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> RouteContractsSectionAsync(Guid id, RouteContractsSectionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AssignContractsSpecialistAsync(Guid id, AssignContractsSpecialistRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AcceptContractsAsync(Guid id, AcceptContractsRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> SelectContractsIntVariantAsync(Guid id, SelectContractsIntVariantRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> CompleteContractsIntStepAsync(Guid id, int step, CompleteContractsIntStepRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AddContractsIntStepFileAsync(Guid id, int step, ContractsIntStepFileInput request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> SubmitContractsIntStepApproversAsync(Guid id, int step, SubmitContractsIntStepApproversRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> DecideContractsIntStepApprovalAsync(Guid id, int step, DecideContractsIntStepApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> SendContractsIntToSecretariatAsync(Guid id, int step, SendContractsIntToSecretariatRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> SelectContractsDomVariantAsync(Guid id, SelectContractsDomVariantRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> CompleteContractsDomStepAsync(Guid id, int step, CompleteContractsDomStepRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ScheduleContractsDomStepAsync(Guid id, int step, ScheduleContractsDomStepRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AddContractsDomStepFileAsync(Guid id, int step, ContractsDomStepFileInput request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> SubmitContractsDomStepApproversAsync(Guid id, int step, SubmitContractsDomStepApproversRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> DecideContractsDomStepApprovalAsync(Guid id, int step, DecideContractsDomStepApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> SendContractsDomToContractsAdminAsync(Guid id, int step, SendContractsDomToContractsAdminRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ReturnContractsDomToMarketingAsync(Guid id, int step, ReturnContractsDomToMarketingRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> RollbackContractsDomStepAsync(Guid id, int step, RollbackContractsDomStepRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetPaymentWorkersAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AssignPaymentSpecialistAsync(Guid id, AssignContractsSpecialistRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AcceptPaymentAsync(Guid id, AcceptContractsRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> SubmitMarketingPlanApprovalAsync(Guid id, SubmitMarketingPlanApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ApproveMarketingPlanAsync(Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> RejectMarketingPlanAsync(Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ConfirmMarketingRegistrationAsync(Guid id, ConfirmMarketingRegistrationRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetMarketingPlanApproverUsersAsync(Guid actorId, string? search, CancellationToken ct = default);
    Task<Result<PagedResult<ProcurementMarketingQueueItemDto>>> GetMarketingQueueAsync(Guid actorId, int page, int pageSize, ProcurementMarketingSubPhase? subPhase, string? search, CancellationToken ct = default);
    Task<Result<ProcurementMarketingQueueSummaryDto>> GetMarketingQueueSummaryAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<PagedResult<ProcurementContractsQueueItemDto>>> GetContractsQueueAsync(Guid actorId, ContractsProcurementSectionType? section, int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementContractsBoardColumnDto>>> GetContractsBoardAsync(Guid actorId, ContractsProcurementSectionType section, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetMarketingWorkersAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AcceptMarketingAsync(Guid id, AcceptMarketingRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AssignMarketingSpecialistAsync(Guid id, AssignMarketingSpecialistRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> ReturnMarketingToInitiatorAsync(Guid id, ReturnMarketingToInitiatorRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> AddStepCommentAsync(Guid id, AddProcurementStepCommentRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> CompleteMarketingAsync(Guid id, MarketingActionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    IReadOnlyList<ProcurementMarketingStepDto> GetMarketingSteps();
    Task<Result<ProcurementRequestDto>> CompleteMarketingStepAsync(Guid id, int step, CompleteMarketingStepRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementRequestDto>> RecordMarketingBranchAsync(Guid id, MarketingBranchRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ProcurementWorkflowRolesAdminDto>> GetWorkflowRolesAdminAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<ProcurementWorkflowRoleDto>> UpdateWorkflowRoleAsync(Guid actorId, string roleKey, UpdateProcurementWorkflowRoleRequest request, string? ip, CancellationToken ct = default);
}

public interface IMarketingService
{
    Task<Result<MarketingRecordDto>> CreateFromProcurementAsync(Guid documentId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> GetByDocumentIdAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<PagedResult<MarketingRecordListItemDto>>> GetRecordsAsync(Guid actorId, MarketingRecordStatus? status, Guid? executorId, MarketingRequestCategory? category, int page, int pageSize, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> SetCategoryAsync(Guid documentId, SetMarketingCategoryRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> AssignExecutorAsync(Guid documentId, AssignMarketingExecutorRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> AcceptAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> ReportTzIssueAsync(Guid documentId, MarketingTzIssueRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> ResolveTzIssueAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> MarkRfqPreparedAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> UploadRfqDocumentAsync(Guid documentId, UploadRfqDocumentRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> RegisterAndGenerateRfqAsync(Guid documentId, RegisterRfqStep3Request request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> RegisterAndGeneratePlanAsync(Guid documentId, RegisterMarketingPlanRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> UploadPlanDocumentAsync(Guid documentId, UploadMarketingPlanDocumentRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<(byte[] Bytes, string FileName)>> DownloadPlanTemplateAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> OpenRfqAtgWebsiteChannelAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> OpenRfqTenderChannelAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> CompleteRfqTenderChannelAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> AddRfqDispatchAsync(Guid documentId, AddRfqDispatchRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingRecordDto>> MarkFollowupSentAsync(Guid dispatchId, MarkRfqFollowupRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingOfferDto>> AddOfferAsync(Guid documentId, AddMarketingOfferRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingOfferDto>> ReviewOfferByInitiatorAsync(Guid offerId, ReviewMarketingOfferRequest request, Guid actorId, CancellationToken ct = default);
    Task<Result<MarketingOfferDto>> ReviewOfferByEngineerAsync(Guid offerId, ReviewMarketingOfferRequest request, Guid actorId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> ValidateStep4ProposalsAsync(Guid documentId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> ValidateStep6PlanAsync(Guid documentId, CancellationToken ct = default);
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
    Task<Result<IncomingLetterDto>> SendToTranslationAsync(Guid id, SendToTranslationRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> CompleteTranslationAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task NotifyHelpDeskTranslationCompletedAsync(Guid ticketId, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> RegisterInEdsAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> SendForResolutionAsync(Guid id, SendForResolutionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> InformAdditionalManagersAsync(Guid id, InformTopManagersRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> RouteToDepartmentAsync(Guid id, RouteIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> AssignWorkerAsync(Guid id, AssignIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> AcceptExecutionAsync(Guid id, AcceptExecutionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> ReportCompletionAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> RequestRevisionAsync(Guid id, IncomingLetterCommentRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> AcceptCompletionAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterDto>> ArchiveAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<IncomingLetterCommentDto>> AddCommentAsync(Guid id, IncomingLetterCommentRequest request, Guid actorId, CancellationToken ct = default);
}

public interface IOutgoingLetterService
{
    Task<Result<OutgoingLetterPermissionsDto>> GetPermissionsAsync(Guid actorId, Guid? documentId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OutgoingLetterUserDto>>> GetDeptHeadsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OutgoingLetterUserDto>>> GetTopManagersAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OutgoingLetterUserDto>>> GetCoordinatorsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> CreateAsync(CreateOutgoingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> UpdateDraftAsync(Guid id, CreateOutgoingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> SendToTranslationAsync(Guid id, SendOutgoingToTranslationRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task NotifyHelpDeskTranslationCompletedAsync(Guid ticketId, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> SubmitToEdsAsync(Guid id, SubmitOutgoingToEdsRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> ApproveDeptHeadAsync(Guid id, OutgoingApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> RejectDeptHeadAsync(Guid id, OutgoingRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> AddCoordinatorsAsync(Guid id, OutgoingCoordinatorRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> CompleteSpecialistCoordinationAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> CompleteDepartmentCoordinationAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> ApproveSupervisingDeputyAsync(Guid id, OutgoingApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> ApproveFirstDeputyAsync(Guid id, OutgoingApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> ApproveGeneralDirectorAsync(Guid id, OutgoingApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> RejectApprovalAsync(Guid id, OutgoingRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> FinalizeEdsAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> SendToRegistrarAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> RegisterAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> ConfirmPaperSignatureAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> ConfirmDispatchAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OutgoingLetterDto>> ArchiveAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
}

public interface IMemoService
{
    Task<Result<MemoPermissionsDto>> GetPermissionsAsync(Guid actorId, Guid? documentId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MemoUserDto>>> GetTopManagersAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MemoUserDto>>> GetDeptHeadsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MemoUserDto>>> GetCoordinatorsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MemoDepartmentDto>>> GetDepartmentsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MemoUserDto>>> GetDepartmentWorkersAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<MemoDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<MemoDto>> CreateAsync(CreateMemoRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> UpdateDraftAsync(Guid id, CreateMemoRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> SendToTranslationAsync(Guid id, SendMemoToTranslationRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task NotifyHelpDeskTranslationCompletedAsync(Guid ticketId, CancellationToken ct = default);
    Task<Result<MemoDto>> SubmitForApprovalAsync(Guid id, SubmitMemoForApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> AddCoordinatorsAsync(Guid id, MemoCoordinatorRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> CompleteSpecialistCoordinationAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> ApproveDeptHeadAsync(Guid id, MemoApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> RejectDeptHeadAsync(Guid id, MemoRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> RegisterAndDistributeAsync(Guid id, RegisterMemoRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> InformRecipientsAsync(Guid id, InformMemoRecipientsRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> RouteToDepartmentAsync(Guid id, RouteMemoRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> AssignWorkerAsync(Guid id, AssignMemoRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> AcceptExecutionAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> ReportCompletionAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> RequestExecutionRevisionAsync(Guid id, MemoCommentRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> AcceptCompletionAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoDto>> ArchiveAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<MemoCommentDto>> AddCommentAsync(Guid id, MemoCommentRequest request, Guid actorId, CancellationToken ct = default);
}

public interface IOrderService
{
    Task<Result<OrderPermissionsDto>> GetPermissionsAsync(Guid actorId, Guid? documentId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OrderUserDto>>> GetDeptHeadsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OrderUserDto>>> GetTopManagersAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OrderUserDto>>> GetCoordinatorsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OrderUserDto>>> GetDistributionTargetsAsync(Guid documentId, Guid actorId, CancellationToken ct = default);
    Task<Result<OrderDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<OrderDto>> CreateAsync(CreateOrderRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> UpdateDraftAsync(Guid id, CreateOrderRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> SubmitForApprovalAsync(Guid id, SubmitOrderRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> AddCoordinatorsAsync(Guid id, OrderCoordinatorRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> CompleteSpecialistCoordinationAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> CompleteDepartmentCoordinationAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> ApproveDeptHeadAsync(Guid id, OrderApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> RejectDeptHeadAsync(Guid id, OrderRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> ApproveLegalAsync(Guid id, OrderApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> RejectLegalAsync(Guid id, OrderRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> ApproveSupervisingDeputyAsync(Guid id, OrderApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> ApproveFirstDeputyAsync(Guid id, OrderApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> ApproveGeneralDirectorAsync(Guid id, OrderApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> RejectApprovalAsync(Guid id, OrderRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> FinalizeEdsAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> SendToRegistrarAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> RegisterAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> ConfirmPaperSignatureAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> UploadScanAsync(Guid id, OrderScanUploadRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> DistributeAsync(Guid id, OrderDistributionRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderDto>> ArchiveAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<OrderCommentDto>> AddCommentAsync(Guid id, OrderCommentRequest request, Guid actorId, CancellationToken ct = default);
}

public interface IHrLeaveRequestService
{
    Task<Result<IReadOnlyList<HrLeaveListItemDto>>> GetMyRequestsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HrLeaveListItemDto>>> GetHrQueueAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<HrLeaveRequestDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<HrLeaveRequestDto>> CreateAsync(CreateHrLeaveRequestRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrLeaveRequestDto>> UpdateAsync(Guid id, UpdateHrLeaveRequestRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrLeaveRequestDto>> SubmitAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrLeaveRequestDto>> HrReviewAsync(Guid id, HrLeaveApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrLeaveRequestDto>> ApproveAsync(Guid id, HrLeaveApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrLeaveRequestDto>> RejectAsync(Guid id, HrLeaveApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrLeaveSigningPackageDto>> GetSigningPackageAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadPdfAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadSignedPdfAsync(Guid id, Guid actorId, string? clientIp, CancellationToken ct = default);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadSignedPkcs7Async(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadJsonSignatureAsync(Guid id, Guid actorId, CancellationToken ct = default);
}

public interface IHrBusinessTripRequestService
{
    Task<Result<IReadOnlyList<HrBusinessTripListItemDto>>> GetMyRequestsAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HrBusinessTripListItemDto>>> GetApprovalQueueAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HrBusinessTripListItemDto>>> GetOrderQueueAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HrBusinessTripListItemDto>>> GetCertificateQueueAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HrBusinessTripColleagueDto>>> GetDepartmentColleaguesAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> CreateAsync(CreateHrBusinessTripRequestRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> UpdateAsync(Guid id, UpdateHrBusinessTripRequestRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> SubmitAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> HrReviewAsync(Guid id, HrBusinessTripApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> ApproveAsync(Guid id, HrBusinessTripApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> RejectAsync(Guid id, HrBusinessTripApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrBusinessTripSigningPackageDto>> GetSigningPackageAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<HrBusinessTripSigningPackageDto>> GetOrderSigningPackageAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> SignOrderWithEimzoAsync(Guid id, HrBusinessTripApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadPdfAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadSignedPdfAsync(Guid id, Guid actorId, string? clientIp, CancellationToken ct = default);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadPresentationPdfAsync(Guid id, Guid actorId, string? clientIp, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> IssueOrderAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrBusinessTripOrderResultDto>> IssueOrdersAsync(IssueHrBusinessTripOrderRequest request, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadOrderDocxAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadOrderPdfPublicAsync(Guid id, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> GenerateCertificatesAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<HrBusinessTripRequestDto>> DeliverCertificatesAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadCertificateAsync(
        Guid id, Guid travelerId, Guid actorId, CancellationToken ct = default);
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
    Task NotifyContractsRoutingRequiredAsync(Guid recipientId, string documentNumber, string documentTitle, Guid documentId, CancellationToken ct = default);
    Task NotifyContractsSectionAssignedAsync(Guid recipientId, string documentNumber, string sectionLabel, Guid documentId, CancellationToken ct = default);
    Task NotifyContractsEngineerAssignedAsync(Guid recipientId, string documentNumber, Guid documentId, CancellationToken ct = default);
    /// <param name="departmentKey">Approval | Marketing | Contracts | ContractsInt | ContractsDom | Payment</param>
    Task NotifyProcurementPhaseMovedAsync(Guid recipientId, string documentNumber, string documentTitle, Guid documentId, string departmentKey, CancellationToken ct = default);
    Task NotifyHrBusinessTripCertificateAvailableAsync(
        Guid recipientId, string documentNumber, string orderNumber, Guid documentId, CancellationToken ct = default);
    Task NotifyItAssetExpiryWarningAsync(
        Guid recipientId, string assetName, DateTime expiresAt, Guid assetId, string category, CancellationToken ct = default);
    Task ProcessApprovalRemindersAsync(CancellationToken ct = default);
    Task<Result<PagedResult<NotificationDto>>> GetInboxAsync(Guid actorId, bool unreadOnly, int page, int pageSize, CancellationToken ct = default);
    Task<Result<NotificationUnreadCountDto>> GetUnreadCountAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<bool>> MarkReadAsync(Guid id, Guid actorId, CancellationToken ct = default);
    Task<Result<bool>> MarkAllReadAsync(Guid actorId, CancellationToken ct = default);
}

public interface IPlatformHomeService
{
    Task<Result<HomeModuleCountsDto>> GetModuleCountsAsync(Guid actorId, CancellationToken ct = default);
}

public interface IHrBusinessTripWorkflowService
{
    Task<IReadOnlyList<HrBusinessTripWorkflowApproverStep>> BuildApprovalChainAsync(
        Guid authorId,
        string workflowDepartmentCode,
        string? authorDepartmentCode,
        Guid organizationId,
        CancellationToken ct = default);

    Task<Result<HrBusinessTripWorkflowAdminDto>> GetAdminAsync(Guid actorId, CancellationToken ct = default);
}

public interface IItAutomationService
{
    Task<Result<ItAutomationHubDto>> GetHubAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<ItAssetDto>>> ListAsync(string? category, int? planYear, CancellationToken ct = default);
    Task<Result<ItAssetDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<ItAssetDto>> CreateAsync(Guid actorId, CreateItAssetRequest request, string? ip, CancellationToken ct = default);
    Task<Result<ItAssetDto>> UpdateAsync(Guid id, Guid actorId, UpdateItAssetRequest request, string? ip, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default);
    Task<Result<ItAutomationRolesAdminDto>> GetRolesAdminAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<ItAutomationRoleDto>> UpdateRoleAsync(Guid actorId, string category, UpdateItAutomationRoleRequest request, string? ip, CancellationToken ct = default);
    Task ProcessExpiryWarningsAsync(CancellationToken ct = default);
}

public interface ILdapService
{
    /// <summary>Authenticates against LDAP/AD. Returns normalized email on success.</summary>
    Task<Result<string>> AuthenticateAsync(string login, string password, CancellationToken ct = default);
}

public interface IEimzoServerClient
{
    Task<EimzoStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<Result<EimzoVerifyResultDto>> VerifyAttachedAsync(string pkcs7Base64, string clientIp, CancellationToken ct = default);
    Task<Result<EimzoVerifyResultDto>> VerifyDetachedAsync(string detachedDataBase64, string pkcs7Base64, string clientIp, CancellationToken ct = default);
    Task<Result<EimzoTimestampResultDto>> AttachTimestampAsync(string pkcs7Base64, string clientIp, CancellationToken ct = default);
}
