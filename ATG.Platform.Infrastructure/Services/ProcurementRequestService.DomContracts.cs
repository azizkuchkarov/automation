using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Dcs;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public partial class ProcurementRequestService
{
    public async Task<Result<ProcurementRequestDto>> SelectContractsDomVariantAsync(
        Guid id, SelectContractsDomVariantRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts)
            return Result<ProcurementRequestDto>.Fail("Request is not at Contracts");
        if (detail.ContractsProcurementSection != ContractsProcurementSectionType.Domestic)
            return Result<ProcurementRequestDto>.Fail("Variant selection is only for Domestic Procurement Section");
        if (detail.ContractsSubPhase != ProcurementContractsSubPhase.InProgress)
            return Result<ProcurementRequestDto>.Fail("Engineer must accept the request before selecting a variant");
        if (detail.ContractsDomVariant is not null)
            return Result<ProcurementRequestDto>.Fail("Procurement variant is already selected");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanSelectContractsDomVariant(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when selecting a variant");

        if (!DomesticContractsDomSteps.IsSupported(request.Variant))
            return Result<ProcurementRequestDto>.Fail("This procurement variant is not available yet");

        detail.ContractsDomVariant = request.Variant;
        detail.ContractsDomVariantSelectedAt = DateTime.UtcNow;
        detail.ContractsDomCurrentStep = DomesticContractsDomSteps.FirstOperationalStep(request.Variant);
        detail.Document.UpdatedAt = DateTime.UtcNow;

        var variantLabel = DomesticContractsDomSteps.VariantLabelRu(request.Variant);
        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, 0,
            $"Variant: {variantLabel} — {request.Comment.Trim()}", ProcurementStepCommentKind.StepCompletion, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "contracts_dom_variant_selected", null,
            detail.Document.Status, variantLabel, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsDomVariantSelected", "Document", id,
            request.Variant.ToString(), ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> CompleteContractsDomStepAsync(
        Guid id, int step, CompleteContractsDomStepRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts)
            return Result<ProcurementRequestDto>.Fail("Request is not at Contracts");
        if (detail.ContractsDomVariant is not { } domVariant
            || !DomesticContractsDomSteps.IsSupported(domVariant))
            return Result<ProcurementRequestDto>.Fail("DOM procurement workflow is not active");
        if (detail.ContractsSubPhase != ProcurementContractsSubPhase.InProgress)
            return Result<ProcurementRequestDto>.Fail("Contracts workflow is not in progress");
        if (detail.ContractsDomCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail($"Expected step {detail.ContractsDomCurrentStep}, not {step}");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<ProcurementRequestDto>.Fail("Access denied");

        var stepDef = DomesticContractsDomSteps.GetDefinitions(domVariant)
            .FirstOrDefault(s => s.Number == step);
        if (stepDef is null) return Result<ProcurementRequestDto>.Fail("Invalid step");

        if (stepDef.RequiresContractsAdmin)
        {
            if (!detail.ContractsDomContractsAdminPending || detail.ContractsDomContractsAdminUserId != actorId)
                return Result<ProcurementRequestDto>.Fail("Only Contracts Administration can complete this step");
        }
        else if (!CanCompleteContractsDomStep(actor, detail))
        {
            return Result<ProcurementRequestDto>.Fail("Access denied");
        }

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when completing a step");

        var totalSteps = DomesticContractsDomSteps.TotalSteps(domVariant);
        if (step < DomesticContractsDomSteps.FirstOperationalStep(domVariant) || step > totalSteps)
            return Result<ProcurementRequestDto>.Fail("Invalid step number");

        if (stepDef.RequiresUpload
            && !detail.ContractsDomStepFiles.Any(f => f.StepNumber == step))
            return Result<ProcurementRequestDto>.Fail("Upload at least one document for this step");

        if (stepDef.RequiresScheduleDate && !HasRequiredDomesticSchedule(detail, domVariant, step))
            return Result<ProcurementRequestDto>.Fail("Set the required workflow date before completing this step");

        if (stepDef.RequiresApprovers)
        {
            var stepApprovers = detail.ContractsDomStepApprovers.Where(a => a.StepNumber == step).ToList();
            if (stepApprovers.Count == 0)
                return Result<ProcurementRequestDto>.Fail("Submit approvers for this step first");
            if (stepApprovers.Any(a => a.Status != ProcurementApproverStatus.Approved))
                return Result<ProcurementRequestDto>.Fail("All approvers must approve before completing this step");
        }

        if (stepDef.RequiresRegistration)
        {
            if (string.IsNullOrWhiteSpace(detail.ContractsDomContractRegistrationNumber))
            {
                detail.ContractsDomContractRegistrationNumber =
                    await ContractsDomRegistrationNumberGenerator.GenerateNextAsync(db, domVariant, ct);
            }
            detail.ContractsDomContractRegisteredAt ??= DateTime.UtcNow;
        }

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, step,
            request.Comment.Trim(), ProcurementStepCommentKind.StepCompletion, ct);

        if (stepDef.RequiresContractsAdmin)
        {
            detail.ContractsDomContractsAdminPending = false;
            if (detail.ContractsSpecialistId is Guid specialistId)
            {
                detail.Document.AssigneeId = specialistId;
                if (detail.ContractsTaskId is Guid contractsTaskId)
                {
                    var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == contractsTaskId, ct);
                    if (task is not null)
                    {
                        task.AssigneeId = specialistId;
                        task.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        if (step >= totalSteps)
        {
            detail.ContractsDomCurrentStep = totalSteps + 1;
            detail.ContractsDomCompletedAt = DateTime.UtcNow;
            detail.ContractsSubPhase = ProcurementContractsSubPhase.Completed;
            await SetWorkTaskStatusAsync(detail.ContractsTaskId, WorkTaskStatus.Done, ct);
            var completedLabel = DomesticContractsDomSteps.VariantLabelEn(domVariant);
            await AddDocumentActivityAsync(detail.Document, actorId, "contracts_dom_completed", null,
                detail.Document.Status, $"{completedLabel} workflow completed", ct);
            if (ShouldHandoffDomesticVariantToPayment(domVariant))
            {
                try
                {
                    await HandoffToPaymentAsync(detail, actorId, ct);
                }
                catch (InvalidOperationException ex)
                {
                    return Result<ProcurementRequestDto>.Fail(ex.Message);
                }
            }
            else
            {
                detail.Phase = ProcurementRequestPhase.Completed;
                detail.PaymentSubPhase = ProcurementPaymentSubPhase.Completed;
                detail.Document.Status = DocumentStatus.Approved;
                detail.Document.AssigneeId = null;
            }
        }
        else
        {
            detail.ContractsDomCurrentStep = step + 1;
            await AddDocumentActivityAsync(detail.Document, actorId, $"contracts_dom_step_{step}_completed", null,
                detail.Document.Status, request.Comment.Trim(), ct);
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsDomStepCompleted", "Document", id, $"step={step}", ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> ScheduleContractsDomStepAsync(
        Guid id, int step, ScheduleContractsDomStepRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsDomCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");
        if (detail.ContractsDomVariant is not { } domVariant
            || !DomesticContractsDomSteps.IsSupported(domVariant))
            return Result<ProcurementRequestDto>.Fail("DOM procurement workflow is not active");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanCompleteContractsDomStep(actor, detail) || detail.ContractsDomContractsAdminPending)
            return Result<ProcurementRequestDto>.Fail("Access denied");

        var stepDef = DomesticContractsDomSteps.GetDefinitions(domVariant).FirstOrDefault(s => s.Number == step);
        if (stepDef is not { RequiresScheduleDate: true })
            return Result<ProcurementRequestDto>.Fail("This step does not require a workflow date");

        var date = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
        switch (domVariant, step)
        {
            case (ContractsDomProcurementVariant.EShop, 5):
                detail.ContractsDomPriceRequestDate = date;
                detail.ContractsDomPriceResponseDueDate = AddBusinessDays(date, 2);
                break;
            case (ContractsDomProcurementVariant.EShop, 7):
                detail.ContractsDomDeliveryDueDate = date;
                break;
            case (ContractsDomProcurementVariant.SmallValue, 6):
                detail.ContractsDomDeliveryDueDate = date;
                break;
            default:
                return Result<ProcurementRequestDto>.Fail("Scheduling is not configured for this step");
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, step,
                request.Comment.Trim(), ProcurementStepCommentKind.Note, ct);
        }

        await AddDocumentActivityAsync(detail.Document, actorId, $"contracts_dom_step_{step}_scheduled", null,
            detail.Document.Status, date.ToString("yyyy-MM-dd"), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsDomStepScheduled", "Document", id, $"step={step}", ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> AddContractsDomStepFileAsync(
        Guid id, int step, ContractsDomStepFileInput request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsDomCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanCompleteContractsDomStep(actor, detail) || detail.ContractsDomContractsAdminPending)
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.StorageKey))
            return Result<ProcurementRequestDto>.Fail("File is required");

        var file = new ProcurementContractsDomStepFile
        {
            Id = Guid.NewGuid(),
            DocumentId = id,
            StepNumber = step,
            FileName = Path.GetFileName(request.FileName.Trim()),
            StorageKey = request.StorageKey.Trim(),
            UploadedById = actorId,
            UploadedAt = DateTime.UtcNow,
        };
        db.ProcurementContractsDomStepFiles.Add(file);
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsDomStepFileUploaded", "Document", id, $"step={step}", ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> SubmitContractsDomStepApproversAsync(
        Guid id, int step, SubmitContractsDomStepApproversRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsDomCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanCompleteContractsDomStep(actor, detail) || detail.ContractsDomContractsAdminPending)
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (request.UserIds is null || request.UserIds.Count == 0)
            return Result<ProcurementRequestDto>.Fail("At least one approver is required");

        var existing = detail.ContractsDomStepApprovers.Where(a => a.StepNumber == step).ToList();
        if (existing.Count > 0)
            return Result<ProcurementRequestDto>.Fail("Approvers already submitted for this step");

        var sort = 0;
        foreach (var userId in request.UserIds.Distinct())
        {
            sort++;
            db.ProcurementContractsDomStepApprovers.Add(new ProcurementContractsDomStepApprover
            {
                Id = Guid.NewGuid(),
                DocumentId = id,
                StepNumber = step,
                UserId = userId,
                SortOrder = sort,
                Status = ProcurementApproverStatus.Pending,
            });
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsDomApproversSubmitted", "Document", id, $"step={step}", ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> DecideContractsDomStepApprovalAsync(
        Guid id, int step, DecideContractsDomStepApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsDomCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");

        var pending = detail.ContractsDomStepApprovers
            .Where(a => a.StepNumber == step && a.Status == ProcurementApproverStatus.Pending)
            .OrderBy(a => a.SortOrder)
            .FirstOrDefault();
        if (pending is null || pending.UserId != actorId)
            return Result<ProcurementRequestDto>.Fail("No pending approval for you on this step");

        if (!request.Approve && string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when rejecting");

        pending.Status = request.Approve
            ? ProcurementApproverStatus.Approved
            : ProcurementApproverStatus.Rejected;
        pending.DecidedAt = DateTime.UtcNow;
        pending.Comment = request.Comment?.Trim();

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsDomStepApproval", "Document", id,
            $"step={step};approve={request.Approve}", ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> SendContractsDomToContractsAdminAsync(
        Guid id, int step, SendContractsDomToContractsAdminRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsDomCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");
        if (detail.ContractsDomContractsAdminPending)
            return Result<ProcurementRequestDto>.Fail("Already sent to Contracts Administration");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanCompleteContractsDomStep(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required");

        var adminUser = await ResolveContractsAdminUserAsync(ct);
        if (adminUser is null)
            return Result<ProcurementRequestDto>.Fail("Contracts Administration user not found (HO-CPROC-CADM)");

        detail.ContractsDomContractsAdminPending = true;
        detail.ContractsDomContractsAdminUserId = adminUser.Id;
        detail.Document.AssigneeId = adminUser.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        if (detail.ContractsTaskId is Guid taskId)
        {
            var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task is not null)
            {
                task.AssigneeId = adminUser.Id;
                if (adminUser.DepartmentId is Guid adminDeptId)
                    task.DepartmentId = adminDeptId;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, step,
            request.Comment.Trim(), ProcurementStepCommentKind.Assignment, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "contracts_dom_sent_to_admin", null,
            detail.Document.Status, adminUser.FullName, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsDomSentToContractsAdmin", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> ReturnContractsDomToMarketingAsync(
        Guid id, int step, ReturnContractsDomToMarketingRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsDomCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");
        if (detail.ContractsDomVariant is not { } domVariant
            || !DomesticContractsDomSteps.IsSupported(domVariant))
            return Result<ProcurementRequestDto>.Fail("DOM procurement workflow is not active");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanCompleteContractsDomStep(actor, detail) || detail.ContractsDomContractsAdminPending)
            return Result<ProcurementRequestDto>.Fail("Access denied");

        var stepDef = DomesticContractsDomSteps.GetDefinitions(domVariant).FirstOrDefault(s => s.Number == step);
        if (stepDef is not { AllowsReturnToMarketing: true })
            return Result<ProcurementRequestDto>.Fail("Return to Marketing is not available on this step");
        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required");

        var marketingHead = await ResolveMarketingSectionHeadAsync(ct);
        var mktSection = await GetDepartmentAsync(HoMktMkt, HoMasterData.OrganizationCode, ct)
            ?? await GetDepartmentAsync(HoMkt, HoMasterData.OrganizationCode, ct);
        if (mktSection is null)
            return Result<ProcurementRequestDto>.Fail("HO Marketing department not found");
        var hoOrg = await db.Organizations.FirstAsync(o => o.Code == HoMasterData.OrganizationCode, ct);

        await SetWorkTaskStatusAsync(detail.ContractsTaskId, WorkTaskStatus.Done, ct);
        var task = await CreateLinkedTaskAsync(
            marketingHead.Id, actorId, mktSection.Id, hoOrg.Id,
            $"Marketing rework — {detail.Document.Number}",
            detail.Document.Title,
            detail.Document.Id, detail.Priority, ct);

        foreach (var file in detail.ContractsDomStepFiles.ToList())
            db.ProcurementContractsDomStepFiles.Remove(file);
        foreach (var approver in detail.ContractsDomStepApprovers.ToList())
            db.ProcurementContractsDomStepApprovers.Remove(approver);
        foreach (var file in detail.ContractsIntStepFiles.ToList())
            db.ProcurementContractsIntStepFiles.Remove(file);
        foreach (var approver in detail.ContractsIntStepApprovers.ToList())
            db.ProcurementContractsIntStepApprovers.Remove(approver);

        ResetDomesticContractsState(detail);
        detail.Phase = ProcurementRequestPhase.Marketing;
        detail.MarketingSubPhase = ProcurementMarketingSubPhase.InProgress;
        detail.MarketingCurrentStep = 2;
        detail.MarketingTaskId = task.Id;
        detail.Document.OrganizationId = hoOrg.Id;
        detail.Document.DepartmentId = mktSection.Id;
        detail.Document.AssigneeId = marketingHead.Id;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, step,
            request.Comment.Trim(), ProcurementStepCommentKind.StepCompletion, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "contracts_dom_returned_to_marketing", null,
            detail.Document.Status, request.Comment.Trim(), ct);

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsDomReturnedToMarketing", "Document", id, $"step={step}", ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> RollbackContractsDomStepAsync(
        Guid id, int step, RollbackContractsDomStepRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsDomCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");
        if (detail.ContractsDomVariant is not { } domVariant
            || !DomesticContractsDomSteps.IsSupported(domVariant))
            return Result<ProcurementRequestDto>.Fail("DOM procurement workflow is not active");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanCompleteContractsDomStep(actor, detail) || detail.ContractsDomContractsAdminPending)
            return Result<ProcurementRequestDto>.Fail("Access denied");

        var stepDef = DomesticContractsDomSteps.GetDefinitions(domVariant).FirstOrDefault(s => s.Number == step);
        if (stepDef is not { AllowsTerminationRollback: true, RollbackStepNumber: not null })
            return Result<ProcurementRequestDto>.Fail("Rollback is not available on this step");
        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required");

        var rollbackStep = stepDef.RollbackStepNumber.Value;
        foreach (var file in detail.ContractsDomStepFiles.Where(f => f.StepNumber >= rollbackStep).ToList())
            db.ProcurementContractsDomStepFiles.Remove(file);
        foreach (var approver in detail.ContractsDomStepApprovers.Where(a => a.StepNumber >= rollbackStep).ToList())
            db.ProcurementContractsDomStepApprovers.Remove(approver);

        detail.ContractsDomCurrentStep = rollbackStep;
        detail.ContractsDomContractsAdminPending = false;
        detail.ContractsDomContractsAdminUserId = null;
        detail.ContractsDomContractRegistrationNumber = null;
        detail.ContractsDomContractRegisteredAt = null;
        detail.ContractsDomDeliveryDueDate = null;
        detail.ContractsDomActualDeliveryDate = null;
        detail.ContractsDomLastTerminationAt = DateTime.UtcNow;
        if (detail.ContractsSpecialistId is Guid specialistId)
            detail.Document.AssigneeId = specialistId;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, step,
            request.Comment.Trim(), ProcurementStepCommentKind.StepCompletion, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "contracts_dom_terminated_and_rolled_back", null,
            detail.Document.Status, $"step={rollbackStep}", ct);

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsDomRolledBack", "Document", id, $"step={rollbackStep}", ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    private async Task<User?> ResolveContractsAdminUserAsync(CancellationToken ct)
    {
        var dept = await GetDepartmentAsync(HoCprocCadm, HoMasterData.OrganizationCode, ct);
        if (dept is null) return null;

        var manager = await db.Users
            .Where(u => u.IsActive && u.DepartmentId == dept.Id && u.Role == UserRole.HONachalnik)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct);
        if (manager is not null) return manager;

        return await db.Users
            .Where(u => u.IsActive && u.DepartmentId == dept.Id)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct);
    }

    private static IReadOnlyList<ProcurementContractsDomStepDto>? BuildContractsDomSteps(ProcurementRequestDetail d)
    {
        if (d.ContractsProcurementSection != ContractsProcurementSectionType.Domestic)
            return null;
        if (d.ContractsDomVariant is not { } variant || !DomesticContractsDomSteps.IsSupported(variant))
            return null;

        var files = d.ContractsDomStepFiles ?? [];
        var approvers = d.ContractsDomStepApprovers ?? [];

        return DomesticContractsDomSteps.GetDefinitions(variant)
            .Select(s =>
            {
                var stepFiles = files.Where(f => f.StepNumber == s.Number)
                    .OrderBy(f => f.UploadedAt)
                    .Select(f => new ProcurementContractsDomStepFileDto(
                        f.Id, f.StepNumber, f.FileName, f.StorageKey,
                        f.UploadedBy?.FullName ?? "—", f.UploadedAt))
                    .ToList();
                var stepApprovers = approvers.Where(a => a.StepNumber == s.Number)
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new ProcurementContractsDomStepApproverDto(
                        a.Id, a.StepNumber, a.UserId, a.User?.FullName ?? "—", a.User?.Email ?? "",
                        a.Status, a.SortOrder, a.DecidedAt, a.Comment))
                    .ToList();
                var submitted = stepApprovers.Count > 0;
                var allApproved = submitted && stepApprovers.All(a => a.Status == ProcurementApproverStatus.Approved);
                var adminPending = s.RequiresContractsAdmin
                    && d.ContractsDomCurrentStep == s.Number
                    && d.ContractsDomContractsAdminPending;
                return new ProcurementContractsDomStepDto(
                    s.Number, s.TitleRu, s.TitleEn, s.HintRu, s.HintEn,
                    s.HasBranch, s.BranchHintRu, s.BranchHintEn,
                    s.RequiresUpload, s.RequiresApprovers, s.RequiresContractsAdmin, s.RequiresRegistration,
                    s.RequiresScheduleDate, s.ScheduleLabelRu, s.ScheduleLabelEn, s.ScheduleHintRu, s.ScheduleHintEn,
                    s.AllowsReturnToMarketing, s.AllowsTerminationRollback, s.RollbackStepNumber,
                    stepFiles, stepApprovers, submitted, allApproved, adminPending);
            })
            .ToList();
    }

    private static bool CanCompleteContractsDomStepNow(ProcurementRequestDetail detail) =>
        detail.ContractsDomVariant is { } v
        && DomesticContractsDomSteps.IsSupported(v)
        && detail.ContractsSubPhase == ProcurementContractsSubPhase.InProgress
        && detail.ContractsDomCurrentStep >= DomesticContractsDomSteps.FirstOperationalStep(v)
        && detail.ContractsDomCurrentStep <= DomesticContractsDomSteps.TotalSteps(v);

    private static ContractsDomStepDefinition? GetCurrentDomStepDefinition(ProcurementRequestDetail detail)
    {
        if (detail.ContractsDomVariant is not { } variant || !DomesticContractsDomSteps.IsSupported(variant))
            return null;
        return DomesticContractsDomSteps.GetDefinitions(variant)
            .FirstOrDefault(s => s.Number == detail.ContractsDomCurrentStep);
    }

    private static bool CanSelectContractsDomVariant(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.ContractsSpecialistId == actor.Id) return true;
        return detail.Document.AssigneeId == actor.Id && IsContractsDomStaff(actor);
    }

    private static bool CanCompleteContractsDomStep(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.ContractsSpecialistId == actor.Id) return true;
        return detail.Document.AssigneeId == actor.Id && IsContractsDomStaff(actor);
    }

    private static bool HasRequiredDomesticSchedule(
        ProcurementRequestDetail detail,
        ContractsDomProcurementVariant variant,
        int step) => (variant, step) switch
    {
        (ContractsDomProcurementVariant.EShop, 5) => detail.ContractsDomPriceRequestDate is not null,
        (ContractsDomProcurementVariant.EShop, 7) => detail.ContractsDomDeliveryDueDate is not null,
        (ContractsDomProcurementVariant.SmallValue, 6) => detail.ContractsDomDeliveryDueDate is not null,
        _ => true,
    };

    private static DateTime AddBusinessDays(DateTime startDate, int businessDays)
    {
        var date = startDate;
        var added = 0;
        while (added < businessDays)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;
            added++;
        }
        return date;
    }

    private static bool ShouldHandoffDomesticVariantToPayment(ContractsDomProcurementVariant variant) =>
        variant is not (ContractsDomProcurementVariant.EShop or ContractsDomProcurementVariant.SmallValue);

    private static void ResetDomesticContractsState(ProcurementRequestDetail detail)
    {
        detail.ContractsSubPhase = ProcurementContractsSubPhase.Pending;
        detail.ContractsProcurementSection = null;
        detail.ContractsSectionRoutedAt = null;
        detail.ContractsSpecialistId = null;
        detail.ContractsAssignedAt = null;
        detail.ContractsAcceptedAt = null;
        detail.ContractsTaskId = null;
        detail.ContractsDomVariant = null;
        detail.ContractsDomCurrentStep = 0;
        detail.ContractsDomVariantSelectedAt = null;
        detail.ContractsDomCompletedAt = null;
        detail.ContractsDomContractRegistrationNumber = null;
        detail.ContractsDomContractRegisteredAt = null;
        detail.ContractsDomContractsAdminPending = false;
        detail.ContractsDomContractsAdminUserId = null;
        detail.ContractsDomPriceRequestDate = null;
        detail.ContractsDomPriceResponseDueDate = null;
        detail.ContractsDomDeliveryDueDate = null;
        detail.ContractsDomActualDeliveryDate = null;
        detail.ContractsIntVariant = null;
        detail.ContractsIntCurrentStep = 0;
        detail.ContractsIntVariantSelectedAt = null;
        detail.ContractsIntCompletedAt = null;
        detail.ContractsIntContractRegistrationNumber = null;
        detail.ContractsIntContractRegisteredAt = null;
        detail.ContractsIntSecretariatPending = false;
        detail.ContractsIntSecretariatUserId = null;
        detail.ContractsDomStepFiles.Clear();
        detail.ContractsDomStepApprovers.Clear();
        detail.ContractsIntStepFiles.Clear();
        detail.ContractsIntStepApprovers.Clear();
    }

    private static bool IsContractsDomStaff(User u) =>
        IsPlatformAdmin(u) || string.Equals(u.Department?.Code, HoCprocDom, StringComparison.Ordinal);

    private static bool IsContractsAdminStaff(User u) =>
        IsPlatformAdmin(u) || string.Equals(u.Department?.Code, HoCprocCadm, StringComparison.Ordinal);
}
