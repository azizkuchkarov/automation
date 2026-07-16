using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Hr;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class HrBusinessTripWorkflowService(AppDbContext db) : IHrBusinessTripWorkflowService
{
    public async Task<IReadOnlyList<HrBusinessTripWorkflowApproverStep>> BuildApprovalChainAsync(
        Guid authorId,
        string workflowDepartmentCode,
        string? authorDepartmentCode,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var workflow = await db.HrBusinessTripDeptWorkflows.AsNoTracking()
            .Include(w => w.Tiers).ThenInclude(t => t.Initiators)
            .Include(w => w.Tiers).ThenInclude(t => t.Steps)
            .FirstOrDefaultAsync(w =>
                w.OrganizationId == organizationId
                && w.DepartmentCode == workflowDepartmentCode
                && w.IsActive, ct);

        if (workflow is null)
            return [];

        var tier = ResolveTier(workflow, authorId);
        if (tier is null)
            return [];

        var chain = new List<HrBusinessTripWorkflowApproverStep>();
        var seen = new HashSet<Guid> { authorId };

        if (tier.PrependsSectionManager)
        {
            var sectionEmail = HrBusinessTripWorkflowResolver.ResolveCprocSectionManagerEmail(authorDepartmentCode);
            if (!string.IsNullOrWhiteSpace(sectionEmail))
            {
                var sectionManagerId = await db.Users.AsNoTracking()
                    .Where(u => u.IsActive && u.Email == sectionEmail)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync(ct);
                if (sectionManagerId != Guid.Empty && seen.Add(sectionManagerId))
                    chain.Add(new HrBusinessTripWorkflowApproverStep(HrBusinessTripApprovalRole.DepartmentHead, sectionManagerId));
            }
        }

        foreach (var step in tier.Steps.OrderBy(s => s.SortOrder))
        {
            if (!seen.Add(step.ApproverUserId))
                continue;

            chain.Add(new HrBusinessTripWorkflowApproverStep(step.Role, step.ApproverUserId));
        }

        return chain;
    }

    public async Task<Result<HrBusinessTripWorkflowAdminDto>> GetAdminAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId, ct);
        if (actor is null || actor.Role is not (UserRole.SuperAdmin or UserRole.HOTopManager))
            return Result<HrBusinessTripWorkflowAdminDto>.Fail("Access denied");

        var hoOrg = await db.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Code == HoMasterData.OrganizationCode, ct);
        if (hoOrg is null)
            return Result<HrBusinessTripWorkflowAdminDto>.Ok(new HrBusinessTripWorkflowAdminDto([], null));

        var workflows = await db.HrBusinessTripDeptWorkflows.AsNoTracking()
            .Where(w => w.OrganizationId == hoOrg.Id)
            .Include(w => w.Tiers).ThenInclude(t => t.Initiators).ThenInclude(i => i.User)
            .Include(w => w.Tiers).ThenInclude(t => t.Steps).ThenInclude(s => s.ApproverUser)
            .OrderBy(w => w.DepartmentCode)
            .ToListAsync(ct);

        var items = workflows.Select(MapWorkflow).ToList();
        return Result<HrBusinessTripWorkflowAdminDto>.Ok(
            new HrBusinessTripWorkflowAdminDto(items, hoOrg.Name));
    }

    private static HrBusinessTripWorkflowTier? ResolveTier(HrBusinessTripDeptWorkflow workflow, Guid authorId)
    {
        var tiers = workflow.Tiers.OrderByDescending(t => t.MatchPriority).ToList();
        foreach (var tier in tiers.Where(t => !t.CatchAllStaff))
        {
            if (tier.Initiators.Any(i => i.UserId == authorId))
                return tier;
        }

        var exclusive = tiers
            .Where(t => !t.CatchAllStaff)
            .SelectMany(t => t.Initiators.Select(i => i.UserId))
            .ToHashSet();

        var catchAll = tiers.FirstOrDefault(t => t.CatchAllStaff);
        if (catchAll is not null && !exclusive.Contains(authorId))
            return catchAll;

        return tiers.FirstOrDefault();
    }

    private static HrBusinessTripDeptWorkflowDto MapWorkflow(HrBusinessTripDeptWorkflow workflow) =>
        new(
            workflow.Id,
            workflow.DepartmentCode,
            workflow.TitleRu,
            workflow.TitleEn,
            workflow.Tiers
                .OrderByDescending(t => t.MatchPriority)
                .Select(t => new HrBusinessTripWorkflowTierDto(
                    t.Id,
                    t.TierKey,
                    t.TitleRu,
                    t.TitleEn,
                    t.MatchPriority,
                    t.CatchAllStaff,
                    t.PrependsSectionManager,
                    t.Initiators
                        .OrderBy(i => i.User.FullName)
                        .Select(i => new HrBusinessTripWorkflowPersonDto(
                            i.UserId,
                            i.User.FullName,
                            i.User.Email))
                        .ToList(),
                    t.Steps
                        .OrderBy(s => s.SortOrder)
                        .Select(s => new HrBusinessTripWorkflowStepDto(
                            s.Id,
                            s.SortOrder,
                            s.ApproverUserId,
                            s.ApproverUser.FullName,
                            s.ApproverUser.Email,
                            s.Role.ToString(),
                            s.LabelRu,
                            s.LabelEn))
                        .ToList()))
                .ToList());
}
