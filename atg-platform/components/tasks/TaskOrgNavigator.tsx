"use client";

import { useState } from "react";
import { useLocale } from "next-intl";
import { TaskNavigationDto, TaskNavigationUnit } from "@/lib/tasks";
import { cn } from "@/lib/utils";
import { Building2, ChevronDown, ChevronRight, Factory, Layers } from "lucide-react";

export interface TaskScopeSelection {
  organizationId?: string;
  departmentId?: string;
  label: string;
}

interface Props {
  navigation: TaskNavigationDto;
  selection: TaskScopeSelection;
  onSelect: (sel: TaskScopeSelection) => void;
}

export function TaskOrgNavigator({ navigation, selection, onSelect }: Props) {
  const locale = useLocale();
  const [expandedOrgs, setExpandedOrgs] = useState<Set<string>>(
    () => new Set(navigation.organizations.map((o) => o.id))
  );
  const [expandedUnits, setExpandedUnits] = useState<Set<string>>(new Set());

  const toggleOrg = (id: string) => {
    setExpandedOrgs((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const toggleUnit = (id: string) => {
    setExpandedUnits((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const isSelected = (orgId?: string, deptId?: string) =>
    deptId ? selection.departmentId === deptId :
    orgId ? selection.organizationId === orgId && !selection.departmentId : false;

  const renderUnit = (unit: TaskNavigationUnit, depth: number, parentOrgId: string) => {
    const hasChildren = unit.children.length > 0;
    const expanded = expandedUnits.has(unit.id);
    const selected =
      unit.unitType === "department"
        ? isSelected(undefined, unit.id)
        : isSelected(unit.id);

    const Icon = unit.unitType === "station" ? Factory : Layers;

    return (
      <div key={unit.id}>
        <button
          type="button"
          onClick={() => {
            if (hasChildren) toggleUnit(unit.id);
            if (unit.unitType === "department") {
              onSelect({ departmentId: unit.id, organizationId: parentOrgId, label: unit.name });
            } else {
              onSelect({ organizationId: unit.id, label: unit.name });
            }
          }}
          className={cn(
            "w-full flex items-center gap-2 py-1.5 pr-2 rounded-lg text-left text-[13px] transition-colors",
            selected ? "bg-atg-amber/15 text-atg-amber font-medium" : "hover:bg-foreground/[0.04] text-foreground/70",
          )}
          style={{ paddingLeft: `${12 + depth * 14}px` }}
        >
          {hasChildren ? (
            expanded ? <ChevronDown size={14} className="shrink-0 opacity-50" /> : <ChevronRight size={14} className="shrink-0 opacity-50" />
          ) : (
            <span className="w-3.5" />
          )}
          <Icon size={14} className="shrink-0 opacity-60" />
          <span className="flex-1 truncate">{locale.startsWith("en") && unit.nameEn ? unit.nameEn : unit.name}</span>
          <span className="text-[10px] tabular-nums bg-foreground/5 px-1.5 py-0.5 rounded">{unit.taskCount}</span>
        </button>
        {hasChildren && expanded && unit.children.map((c) => renderUnit(c, depth + 1, parentOrgId))}
      </div>
    );
  };

  return (
    <div className="rounded-2xl border border-border/80 bg-surface shadow-sm overflow-hidden">
      <div className="px-4 py-3 border-b border-border/60 bg-foreground/[0.02]">
        <p className="text-xs font-semibold uppercase tracking-wider text-foreground/45">Organization</p>
      </div>
      <div className="p-2 max-h-[calc(100vh-14rem)] overflow-y-auto">
        <button
          type="button"
          onClick={() => onSelect({ label: "Enterprise" })}
          className={cn(
            "w-full flex items-center gap-2 px-3 py-2 rounded-lg text-[13px] font-medium mb-1 transition-colors",
            !selection.organizationId && !selection.departmentId
              ? "bg-atg-amber/15 text-atg-amber"
              : "hover:bg-foreground/[0.04] text-foreground/70"
          )}
        >
          <Building2 size={15} />
          <span className="flex-1 text-left">All</span>
        </button>

        {navigation.organizations.map((org) => {
          const orgExpanded = expandedOrgs.has(org.id);
          const orgSelected = isSelected(org.id);
          return (
            <div key={org.id} className="mb-1">
              <button
                type="button"
                onClick={() => {
                  toggleOrg(org.id);
                  onSelect({ organizationId: org.id, label: org.name });
                }}
                className={cn(
                  "w-full flex items-center gap-2 px-3 py-2 rounded-lg text-[13px] font-semibold transition-colors",
                  orgSelected && !selection.departmentId
                    ? "bg-atg-amber/15 text-atg-amber"
                    : "hover:bg-foreground/[0.04] text-foreground/85"
                )}
              >
                {orgExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                <Building2 size={15} className="text-atg-amber" />
                <span className="flex-1 text-left truncate">{org.name}</span>
                <span className="text-[10px] tabular-nums font-bold bg-atg-amber/10 text-atg-amber px-1.5 py-0.5 rounded">
                  {org.taskCount}
                </span>
              </button>
              {orgExpanded && org.units.map((u) => renderUnit(u, 0, org.id))}
            </div>
          );
        })}
      </div>
    </div>
  );
}
