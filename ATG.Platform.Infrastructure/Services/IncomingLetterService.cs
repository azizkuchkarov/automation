using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Dcs;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class IncomingLetterService(AppDbContext db, IAuditService audit) : IIncomingLetterService
{
  private const string ClericalDept = "HO-DCPR-CLER";

  public async Task<Result<IncomingLetterPermissionsDto>> GetPermissionsAsync(
      Guid actorId, Guid? documentId, CancellationToken ct = default)
  {
    var actor = await GetActorAsync(actorId, ct);
    if (actor is null) return Result<IncomingLetterPermissionsDto>.Fail("User not found");

    IncomingLetterDetail? detail = null;
    if (documentId.HasValue)
      detail = await LoadDetailAsync(documentId.Value, ct);

    var isRegistrar = IsRegistrar(actor);
    var phase = detail?.Phase ?? IncomingLetterPhase.Registered;

    var isRecipient = detail?.Recipients.Any(r => r.UserId == actorId) == true;
    var isAssignee = detail?.Document.AssigneeId == actorId;
    var isRoutedDeptManager = detail is not null && IsDeptManager(actor) &&
        actor.DepartmentId == detail.Document.DepartmentId;

    return Result<IncomingLetterPermissionsDto>.Ok(new IncomingLetterPermissionsDto(
        isRegistrar,
        isRegistrar && phase == IncomingLetterPhase.Registered,
        actor.Role == UserRole.HOTopManager && isRecipient && phase == IncomingLetterPhase.Informed,
        isRoutedDeptManager && phase == IncomingLetterPhase.RoutedToDepartment,
        (isAssignee || isRoutedDeptManager) && phase == IncomingLetterPhase.InExecution,
        CanView(actor, detail)));
  }

  public async Task<Result<IReadOnlyList<IncomingLetterUserDto>>> GetTopManagersAsync(
      Guid actorId, CancellationToken ct = default)
  {
    var actor = await GetActorAsync(actorId, ct);
    if (actor is null || !IsRegistrar(actor))
      return Result<IReadOnlyList<IncomingLetterUserDto>>.Fail("Access denied");

    var users = await db.Users.AsNoTracking()
        .Include(u => u.Department)
        .Where(u => u.IsActive && u.Role == UserRole.HOTopManager &&
                    u.Organization.Code == HoMasterData.OrganizationCode)
        .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
        .ToListAsync(ct);

    return Result<IReadOnlyList<IncomingLetterUserDto>>.Ok(users.Select(MapUser).ToList());
  }

  public async Task<Result<IReadOnlyList<IncomingLetterDepartmentDto>>> GetDepartmentsAsync(
      Guid actorId, CancellationToken ct = default)
  {
    var actor = await GetActorAsync(actorId, ct);
    if (actor is null || actor.Role != UserRole.HOTopManager)
      return Result<IReadOnlyList<IncomingLetterDepartmentDto>>.Fail("Access denied");

    var depts = await db.Departments.AsNoTracking()
        .Include(d => d.Organization)
        .Where(d => d.IsActive && d.Organization.Code == HoMasterData.OrganizationCode)
        .OrderBy(d => d.Code)
        .ToListAsync(ct);

    return Result<IReadOnlyList<IncomingLetterDepartmentDto>>.Ok(
        depts.Select(d => new IncomingLetterDepartmentDto(d.Id, d.Code, d.Name, d.NameEn)).ToList());
  }

  public async Task<Result<IReadOnlyList<IncomingLetterUserDto>>> GetDepartmentWorkersAsync(
      Guid documentId, Guid actorId, CancellationToken ct = default)
  {
    var detail = await LoadDetailAsync(documentId, ct);
    if (detail is null) return Result<IReadOnlyList<IncomingLetterUserDto>>.Fail("Letter not found");

    var actor = await GetActorAsync(actorId, ct);
    if (actor is null || !IsDeptManager(actor) || actor.DepartmentId != detail.Document.DepartmentId)
      return Result<IReadOnlyList<IncomingLetterUserDto>>.Fail("Access denied");

    var users = await db.Users.AsNoTracking()
        .Include(u => u.Department)
        .Where(u => u.IsActive && u.DepartmentId == detail.Document.DepartmentId)
        .OrderBy(u => u.LastName)
        .ToListAsync(ct);

    return Result<IReadOnlyList<IncomingLetterUserDto>>.Ok(users.Select(MapUser).ToList());
  }

  public async Task<Result<IncomingLetterDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default)
  {
    var detail = await LoadDetailAsync(id, ct);
    if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

    var actor = await GetActorAsync(actorId, ct);
    if (actor is null || !CanView(actor, detail))
      return Result<IncomingLetterDto>.Fail("Access denied");

    return Result<IncomingLetterDto>.Ok(MapDetail(detail));
  }

  public async Task<Result<IncomingLetterDto>> CreateAsync(
      CreateIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default)
  {
    var actor = await GetActorAsync(actorId, ct);
    if (actor is null) return Result<IncomingLetterDto>.Fail("User not found");
    if (!IsRegistrar(actor))
      return Result<IncomingLetterDto>.Fail("Only the designated registrar can register incoming letters");

    if (string.IsNullOrWhiteSpace(request.Title))
      return Result<IncomingLetterDto>.Fail("Subject is required");

    var dept = await db.Departments.FirstOrDefaultAsync(d =>
        d.Code == ClericalDept && d.Organization.Code == HoMasterData.OrganizationCode, ct);
    if (dept is null) return Result<IncomingLetterDto>.Fail("Clerical department not found");

    var number = await GenerateNumberAsync(ct);
    var doc = new Document
    {
      Id = Guid.NewGuid(),
      Number = number,
      Title = request.Title.Trim(),
      TitleRu = request.TitleRu?.Trim(),
      Type = DocumentType.Incoming,
      Status = DocumentStatus.Registered,
      AuthorId = actorId,
      OrganizationId = actor.OrganizationId,
      DepartmentId = dept.Id,
      IncomingNumber = request.IncomingNumber?.Trim(),
      IncomingDate = request.IncomingDate,
      RecordBook = request.RecordBook?.Trim(),
      SenderName = request.SenderName?.Trim(),
      ReceiverName = request.ReceiverName?.Trim(),
      AttachmentFileName = request.AttachmentFileName?.Trim(),
      TranslationRequestCount = request.TranslationRequestCount,
      RegisteredAt = DateTime.UtcNow,
    };

    var letter = new IncomingLetterDetail
    {
      DocumentId = doc.Id,
      Document = doc,
      Phase = IncomingLetterPhase.Registered,
    };

    db.Documents.Add(doc);
    db.IncomingLetterDetails.Add(letter);
    await AddActivityAsync(doc, actorId, "registered", null, DocumentStatus.Registered, null, ct);
    await db.SaveChangesAsync(ct);
    await audit.LogAsync(actorId, "IncomingLetterCreated", "Document", doc.Id, number, ip, ct);

    return await GetByIdAsync(doc.Id, actorId, ct);
  }

  public async Task<Result<IncomingLetterDto>> InformTopManagersAsync(
      Guid id, InformTopManagersRequest request, Guid actorId, string? ip, CancellationToken ct = default)
  {
    var detail = await LoadDetailTrackedAsync(id, ct);
    if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

    var actor = await GetActorAsync(actorId, ct);
    if (actor is null || !IsRegistrar(actor))
      return Result<IncomingLetterDto>.Fail("Access denied");
    if (detail.Phase != IncomingLetterPhase.Registered)
      return Result<IncomingLetterDto>.Fail("Letter was already distributed");
    if (request.TopManagerIds.Count == 0)
      return Result<IncomingLetterDto>.Fail("Select at least one top manager");

    foreach (var userId in request.TopManagerIds.Distinct())
    {
      var tm = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.Role == UserRole.HOTopManager, ct);
      if (tm is null) return Result<IncomingLetterDto>.Fail("Invalid top manager selected");

      var task = await CreateLinkedTaskAsync(
          tm.Id, actorId, tm.DepartmentId ?? detail.Document.DepartmentId, tm.OrganizationId,
          $"Incoming letter {detail.Document.Number}",
          detail.Document.Title,
          detail.DocumentId, ct);

      detail.Recipients.Add(new IncomingLetterRecipient
      {
        Id = Guid.NewGuid(),
        DocumentId = detail.DocumentId,
        UserId = tm.Id,
        Informed = true,
        InformedAt = DateTime.UtcNow,
        TaskId = task.Id,
      });
    }

    detail.Phase = IncomingLetterPhase.Informed;
    detail.InformedAt = DateTime.UtcNow;
    detail.Document.Status = DocumentStatus.InReview;
    detail.Document.UpdatedAt = DateTime.UtcNow;

    await AddActivityAsync(detail.Document, actorId, "informed_top_managers", null,
        DocumentStatus.InReview, $"{request.TopManagerIds.Count} top manager(s)", ct);
    await db.SaveChangesAsync(ct);
    await audit.LogAsync(actorId, "IncomingLetterInformed", "Document", id, null, ip, ct);

    return await GetByIdAsync(id, actorId, ct);
  }

  public async Task<Result<IncomingLetterDto>> RouteToDepartmentAsync(
      Guid id, RouteIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default)
  {
    var detail = await LoadDetailTrackedAsync(id, ct);
    if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

    var actor = await GetActorAsync(actorId, ct);
    if (actor is null || actor.Role != UserRole.HOTopManager)
      return Result<IncomingLetterDto>.Fail("Access denied");
    if (!detail.Recipients.Any(r => r.UserId == actorId))
      return Result<IncomingLetterDto>.Fail("You are not a recipient of this letter");
    if (detail.Phase != IncomingLetterPhase.Informed)
      return Result<IncomingLetterDto>.Fail("Letter is not awaiting routing");

    var dept = await db.Departments.Include(d => d.Organization)
        .FirstOrDefaultAsync(d => d.Id == request.TargetDepartmentId && d.IsActive, ct);
    if (dept is null) return Result<IncomingLetterDto>.Fail("Department not found");

    var deptHead = await db.Users
        .Where(u => u.IsActive && u.DepartmentId == dept.Id && u.Role == UserRole.HONachalnik)
        .OrderBy(u => u.LastName)
        .FirstOrDefaultAsync(ct)
        ?? await db.Users
            .Where(u => u.IsActive && u.DepartmentId == dept.Id)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct);

    if (deptHead is null) return Result<IncomingLetterDto>.Fail("No staff in target department");

    await CreateLinkedTaskAsync(
        deptHead.Id, actorId, dept.Id, dept.OrganizationId,
        $"Incoming letter — {dept.NameEn ?? dept.Name}",
        detail.Document.Title,
        detail.DocumentId, ct);

    if (!string.IsNullOrWhiteSpace(request.Comment))
    {
      detail.Comments.Add(new IncomingLetterComment
      {
        Id = Guid.NewGuid(),
        DocumentId = detail.DocumentId,
        AuthorId = actorId,
        Body = request.Comment.Trim(),
      });
    }

    detail.Phase = IncomingLetterPhase.RoutedToDepartment;
    detail.RoutedById = actorId;
    detail.RoutedToDepartmentId = dept.Id;
    detail.RoutedAt = DateTime.UtcNow;
    detail.Document.DepartmentId = dept.Id;
    detail.Document.UpdatedAt = DateTime.UtcNow;

    await AddActivityAsync(detail.Document, actorId, "routed_to_department", null,
        detail.Document.Status, dept.Code, ct);
    await db.SaveChangesAsync(ct);
    await audit.LogAsync(actorId, "IncomingLetterRouted", "Document", id, dept.Code, ip, ct);

    return await GetByIdAsync(id, actorId, ct);
  }

  public async Task<Result<IncomingLetterDto>> AssignWorkerAsync(
      Guid id, AssignIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default)
  {
    var detail = await LoadDetailTrackedAsync(id, ct);
    if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

    var actor = await GetActorAsync(actorId, ct);
    if (actor is null || !IsDeptManager(actor) || actor.DepartmentId != detail.Document.DepartmentId)
      return Result<IncomingLetterDto>.Fail("Access denied");
    if (detail.Phase != IncomingLetterPhase.RoutedToDepartment)
      return Result<IncomingLetterDto>.Fail("Letter is not ready for assignment");

    var worker = await db.Users.FirstOrDefaultAsync(u =>
        u.Id == request.AssigneeId && u.IsActive && u.DepartmentId == detail.Document.DepartmentId, ct);
    if (worker is null) return Result<IncomingLetterDto>.Fail("Assignee must be in your department");

    await CreateLinkedTaskAsync(
        worker.Id, actorId, detail.Document.DepartmentId, detail.Document.OrganizationId,
        $"Execute incoming letter {detail.Document.Number}",
        detail.Document.Title,
        detail.DocumentId, ct);

    if (!string.IsNullOrWhiteSpace(request.Comment))
    {
      detail.Comments.Add(new IncomingLetterComment
      {
        Id = Guid.NewGuid(),
        DocumentId = detail.DocumentId,
        AuthorId = actorId,
        Body = request.Comment.Trim(),
      });
    }

    detail.Document.AssigneeId = worker.Id;
    detail.Phase = IncomingLetterPhase.InExecution;
    detail.Document.Status = DocumentStatus.InReview;
    detail.Document.UpdatedAt = DateTime.UtcNow;

    await AddActivityAsync(detail.Document, actorId, "assigned", null,
        DocumentStatus.InReview, worker.FullName, ct);
    await db.SaveChangesAsync(ct);
    await audit.LogAsync(actorId, "IncomingLetterAssigned", "Document", id, worker.FullName, ip, ct);

    return await GetByIdAsync(id, actorId, ct);
  }

  public async Task<Result<IncomingLetterDto>> CompleteAsync(
      Guid id, Guid actorId, string? ip, CancellationToken ct = default)
  {
    var detail = await LoadDetailTrackedAsync(id, ct);
    if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

    var actor = await GetActorAsync(actorId, ct);
    if (actor is null) return Result<IncomingLetterDto>.Fail("User not found");

    var canComplete = detail.Document.AssigneeId == actorId ||
        (IsDeptManager(actor) && actor.DepartmentId == detail.Document.DepartmentId);
    if (!canComplete || detail.Phase != IncomingLetterPhase.InExecution)
      return Result<IncomingLetterDto>.Fail("Access denied");

    detail.Phase = IncomingLetterPhase.Completed;
    detail.CompletedAt = DateTime.UtcNow;
    detail.Document.Status = DocumentStatus.Approved;
    detail.Document.UpdatedAt = DateTime.UtcNow;

    await AddActivityAsync(detail.Document, actorId, "completed", DocumentStatus.InReview,
        DocumentStatus.Approved, null, ct);
    await db.SaveChangesAsync(ct);
    await audit.LogAsync(actorId, "IncomingLetterCompleted", "Document", id, null, ip, ct);

    return await GetByIdAsync(id, actorId, ct);
  }

  public async Task<Result<IncomingLetterCommentDto>> AddCommentAsync(
      Guid id, IncomingLetterCommentRequest request, Guid actorId, CancellationToken ct = default)
  {
    var detail = await LoadDetailTrackedAsync(id, ct);
    if (detail is null) return Result<IncomingLetterCommentDto>.Fail("Letter not found");

    var actor = await GetActorAsync(actorId, ct);
    if (actor is null || !CanView(actor, detail))
      return Result<IncomingLetterCommentDto>.Fail("Access denied");
    if (string.IsNullOrWhiteSpace(request.Body))
      return Result<IncomingLetterCommentDto>.Fail("Comment is required");

    var comment = new IncomingLetterComment
    {
      Id = Guid.NewGuid(),
      DocumentId = detail.DocumentId,
      AuthorId = actorId,
      Body = request.Body.Trim(),
    };
    detail.Comments.Add(comment);
    detail.Document.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    await audit.LogAsync(actorId, "IncomingLetterComment", "Document", id, null, null, ct);

    return Result<IncomingLetterCommentDto>.Ok(new IncomingLetterCommentDto(
        comment.Id, actorId, actor.FullName, comment.Body, comment.CreatedAt));
  }

  private async Task<IncomingLetterDetail?> LoadDetailAsync(Guid id, CancellationToken ct) =>
      await db.IncomingLetterDetails.AsNoTracking()
          .Include(d => d.Document).ThenInclude(doc => doc.Author)
          .Include(d => d.Document).ThenInclude(doc => doc.Assignee)
          .Include(d => d.Document).ThenInclude(doc => doc.Department)
          .Include(d => d.RoutedBy)
          .Include(d => d.RoutedToDepartment)
          .Include(d => d.Recipients).ThenInclude(r => r.User)
          .Include(d => d.Comments).ThenInclude(c => c.Author)
          .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

  private async Task<IncomingLetterDetail?> LoadDetailTrackedAsync(Guid id, CancellationToken ct) =>
      await db.IncomingLetterDetails
          .Include(d => d.Document)
          .Include(d => d.Recipients)
          .Include(d => d.Comments)
          .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

  private static IncomingLetterDto MapDetail(IncomingLetterDetail d) => new(
      d.DocumentId,
      d.Document.Number,
      d.Document.Title,
      d.Document.TitleRu,
      d.Document.Status,
      d.Phase,
      d.Document.AuthorId,
      d.Document.Author.FullName,
      d.Document.IncomingNumber,
      d.Document.IncomingDate,
      d.Document.RecordBook,
      d.Document.SenderName,
      d.Document.ReceiverName,
      d.Document.AttachmentFileName,
      d.Document.TranslationRequestCount,
      d.Document.OrganizationId,
      d.Document.DepartmentId,
      d.Document.Department.Name,
      d.Document.Department.NameEn,
      d.Document.AssigneeId,
      d.Document.Assignee?.FullName,
      d.RoutedToDepartmentId,
      d.RoutedToDepartment?.Name,
      d.RoutedToDepartment?.NameEn,
      d.RoutedBy?.FullName,
      d.Document.RegisteredAt,
      d.InformedAt,
      d.RoutedAt,
      d.CompletedAt,
      d.Recipients.OrderBy(r => r.InformedAt).Select(r => new IncomingLetterRecipientDto(
          r.Id, r.UserId, r.User.FullName, r.Informed, r.InformedAt, r.TaskId)).ToList(),
      d.Comments.OrderBy(c => c.CreatedAt).Select(c => new IncomingLetterCommentDto(
          c.Id, c.AuthorId, c.Author.FullName, c.Body, c.CreatedAt)).ToList(),
      d.Document.CreatedAt,
      d.Document.UpdatedAt);

  private static IncomingLetterUserDto MapUser(User u) => new(
      u.Id, u.FullName, u.Email, u.EmployeeId,
      u.Department?.Name ?? "", u.Department?.NameEn ?? "");

  private async Task<WorkTask> CreateLinkedTaskAsync(
      Guid assigneeId, Guid createdById, Guid deptId, Guid orgId,
      string title, string description, Guid documentId, CancellationToken ct)
  {
    var task = new WorkTask
    {
      Id = Guid.NewGuid(),
      Number = await GenerateTaskNumberAsync(ct),
      Title = title,
      Description = description,
      Status = WorkTaskStatus.New,
      Priority = TaskPriority.Medium,
      Source = TaskSource.DCS,
      ExternalId = documentId,
      AssigneeId = assigneeId,
      CreatedById = createdById,
      OrganizationId = orgId,
      DepartmentId = deptId,
    };
    db.WorkTasks.Add(task);
    return task;
  }

  private async Task<string> GenerateNumberAsync(CancellationToken ct)
  {
    var prefix = $"{DcsRouting.NumberPrefix(DocumentType.Incoming)}-{DateTime.UtcNow.Year}-";
    var last = await db.Documents.Where(d => d.Number.StartsWith(prefix))
        .OrderByDescending(d => d.Number).Select(d => d.Number).FirstOrDefaultAsync(ct);
    var seq = 1;
    if (last is not null && int.TryParse(last[(prefix.Length)..], out var n)) seq = n + 1;
    return $"{prefix}{seq:D4}";
  }

  private async Task<string> GenerateTaskNumberAsync(CancellationToken ct)
  {
    var prefix = $"TSK-{DateTime.UtcNow.Year}-";
    var last = await db.WorkTasks.Where(t => t.Number.StartsWith(prefix))
        .OrderByDescending(t => t.Number).Select(t => t.Number).FirstOrDefaultAsync(ct);
    var seq = 1;
    if (last is not null && int.TryParse(last[(prefix.Length)..], out var n)) seq = n + 1;
    return $"{prefix}{seq:D4}";
  }

  private async Task AddActivityAsync(
      Document doc, Guid actorId, string action, DocumentStatus? from, DocumentStatus? to, string? details, CancellationToken ct)
  {
    db.DocumentActivities.Add(new DocumentActivity
    {
      Id = Guid.NewGuid(),
      DocumentId = doc.Id,
      ActorId = actorId,
      Action = action,
      FromStatus = from,
      ToStatus = to,
      Details = details,
    });
    await Task.CompletedTask;
  }

  private async Task<User?> GetActorAsync(Guid id, CancellationToken ct) =>
      await db.Users.Include(u => u.Organization).Include(u => u.Department)
          .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);

  private static bool IsRegistrar(User u) =>
      u.Email.Equals(DcsRouting.IncomingRegistrarEmail, StringComparison.OrdinalIgnoreCase) ||
      u.Role == UserRole.SuperAdmin;

  private static bool IsDeptManager(User u) =>
      u.Role is UserRole.HONachalnik or UserRole.BMGMCNachalnikiOtdeli or UserRole.BMGMCManager;

  private static bool CanView(User actor, IncomingLetterDetail? detail)
  {
    if (detail is null) return IsRegistrar(actor) || actor.Role == UserRole.SuperAdmin;
    if (IsRegistrar(actor) || actor.Role == UserRole.SuperAdmin) return true;
    if (detail.Document.AuthorId == actor.Id) return true;
    if (detail.Recipients.Any(r => r.UserId == actor.Id)) return true;
    if (detail.Document.AssigneeId == actor.Id) return true;
    if (IsDeptManager(actor) && actor.DepartmentId == detail.Document.DepartmentId) return true;
    if (actor.Role == UserRole.HOTopManager) return true;
    return false;
  }
}
