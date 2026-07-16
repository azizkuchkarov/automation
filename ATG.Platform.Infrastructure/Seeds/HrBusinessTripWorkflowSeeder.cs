using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATG.Platform.Infrastructure.Seeds;

public static class HrBusinessTripWorkflowSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hoOrg = await db.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Code == HoMasterData.OrganizationCode);
        if (hoOrg is null)
            return;

        var usersByEmail = await db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .ToDictionaryAsync(u => u.Email.ToLowerInvariant(), u => u.Id);

        foreach (var def in HoBusinessTripWorkflowDefinitions.All)
        {
            var existing = await db.HrBusinessTripDeptWorkflows
                .Include(w => w.Tiers).ThenInclude(t => t.Initiators)
                .Include(w => w.Tiers).ThenInclude(t => t.Steps)
                .FirstOrDefaultAsync(w => w.OrganizationId == hoOrg.Id && w.DepartmentCode == def.Code);

            if (existing is not null)
            {
                foreach (var tier in existing.Tiers)
                {
                    db.HrBusinessTripWorkflowInitiators.RemoveRange(tier.Initiators);
                    db.HrBusinessTripWorkflowSteps.RemoveRange(tier.Steps);
                }
                db.HrBusinessTripWorkflowTiers.RemoveRange(existing.Tiers);
                existing.TitleRu = def.TitleRu;
                existing.TitleEn = def.TitleEn;
                existing.UpdatedAt = DateTime.UtcNow;
                AddTiers(db, existing.Id, def, usersByEmail);
                continue;
            }

            var workflow = new HrBusinessTripDeptWorkflow
            {
                Id = Guid.NewGuid(),
                OrganizationId = hoOrg.Id,
                DepartmentCode = def.Code,
                TitleRu = def.TitleRu,
                TitleEn = def.TitleEn,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow,
            };
            db.HrBusinessTripDeptWorkflows.Add(workflow);
            AddTiers(db, workflow.Id, def, usersByEmail);
        }

        await db.SaveChangesAsync();
    }

    private static void AddTiers(
        AppDbContext db,
        Guid workflowId,
        HoBusinessTripWorkflowDefinitions.DeptDef def,
        Dictionary<string, Guid> usersByEmail)
    {
        foreach (var tierDef in def.Tiers)
        {
            var tier = new HrBusinessTripWorkflowTier
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflowId,
                TierKey = tierDef.Key,
                TitleRu = tierDef.TitleRu,
                TitleEn = tierDef.TitleEn,
                MatchPriority = tierDef.Priority,
                CatchAllStaff = tierDef.CatchAllStaff,
                PrependsSectionManager = tierDef.PrependsSectionManager,
            };
            db.HrBusinessTripWorkflowTiers.Add(tier);

            if (tierDef.InitiatorEmails is not null)
            {
                foreach (var email in tierDef.InitiatorEmails)
                {
                    if (!usersByEmail.TryGetValue(email.ToLowerInvariant(), out var userId))
                        continue;

                    db.HrBusinessTripWorkflowInitiators.Add(new HrBusinessTripWorkflowInitiator
                    {
                        Id = Guid.NewGuid(),
                        TierId = tier.Id,
                        UserId = userId,
                    });
                }
            }

            var order = 0;
            foreach (var email in tierDef.StepEmails)
            {
                if (!usersByEmail.TryGetValue(email.ToLowerInvariant(), out var approverId))
                    continue;

                db.HrBusinessTripWorkflowSteps.Add(new HrBusinessTripWorkflowStep
                {
                    Id = Guid.NewGuid(),
                    TierId = tier.Id,
                    SortOrder = order++,
                    ApproverUserId = approverId,
                    Role = MapRole(email, order - 1),
                });
            }
        }
    }

    internal static HrBusinessTripApprovalRole MapRole(string email, int index)
    {
        if (string.Equals(email, HoBusinessTripWorkflowDefinitions.Fdgd, StringComparison.OrdinalIgnoreCase))
            return HrBusinessTripApprovalRole.FirstDeputyGeneralDirector;
        if (string.Equals(email, HoBusinessTripWorkflowDefinitions.Gd, StringComparison.OrdinalIgnoreCase))
            return HrBusinessTripApprovalRole.GeneralDirector;

        return index % 2 == 0
            ? HrBusinessTripApprovalRole.DeputyDepartmentHead
            : HrBusinessTripApprovalRole.DepartmentHead;
    }
}
