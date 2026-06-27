"use client";

import { useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import {
  Building2,
  ChevronDown,
  ChevronRight,
  Factory,
  GitBranch,
  Landmark,
  Layers,
  Network,
  Search,
  Users,
  X,
} from "lucide-react";
import { cn } from "@/lib/utils";

export interface DeptNode {
  id: string;
  name: string;
  nameEn: string;
  code: string;
  isActive: boolean;
  userCount: number;
  totalUserCount: number;
  children: DeptNode[];
}

export interface OrgNode {
  id: string;
  name: string;
  code: string;
  orgType: string;
  isActive: boolean;
  userCount: number;
  totalUserCount: number;
  departments: DeptNode[];
  children: OrgNode[];
}

function deptLabel(node: DeptNode, locale: string) {
  return locale.startsWith("en") && node.nameEn ? node.nameEn : node.name;
}

function deptSubLabel(node: DeptNode, locale: string) {
  if (!node.nameEn || locale.startsWith("en")) return node.name;
  return node.nameEn;
}

const ORG_STYLES: Record<string, { icon: typeof Landmark; accent: string; ring: string; glow: string }> = {
  HeadOffice: {
    icon: Landmark,
    accent: "from-blue-600/90 to-indigo-700/90",
    ring: "ring-blue-500/30",
    glow: "shadow-blue-500/10",
  },
  BMGMC: {
    icon: Building2,
    accent: "from-teal-600/90 to-cyan-700/90",
    ring: "ring-teal-500/30",
    glow: "shadow-teal-500/10",
  },
  Station: {
    icon: Factory,
    accent: "from-violet-600/90 to-purple-700/90",
    ring: "ring-violet-500/30",
    glow: "shadow-violet-500/10",
  },
};

function matchesSearch(node: { name: string; nameEn?: string; code: string }, q: string) {
  if (!q) return true;
  const lower = q.toLowerCase();
  return (
    node.name.toLowerCase().includes(lower) ||
    (node.nameEn?.toLowerCase().includes(lower) ?? false) ||
    node.code.toLowerCase().includes(lower)
  );
}

function SectionCard({
  node,
  index,
  locale,
  search,
}: {
  node: DeptNode;
  index: number;
  locale: string;
  search: string;
}) {
  if (!matchesSearch(node, search)) return null;

  return (
    <div className="relative flex gap-3 group">
      <div className="flex flex-col items-center w-8 shrink-0 pt-3">
        <div className="w-2 h-2 rounded-full bg-amber-500/80 ring-4 ring-amber-500/15" />
        <div className="w-px flex-1 bg-gradient-to-b from-amber-500/40 to-transparent min-h-[8px]" />
      </div>
      <div className="flex-1 mb-2 rounded-xl border border-amber-500/20 bg-gradient-to-br from-amber-500/8 via-surface to-surface p-3.5 shadow-sm hover:border-amber-500/35 hover:shadow-md transition-all duration-200">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2 mb-1">
              <span className="text-[10px] font-bold tracking-widest text-amber-500/80 tabular-nums">
                {String(index).padStart(2, "0")}
              </span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-amber-500/10 text-amber-600 dark:text-amber-400">
                {node.code}
              </span>
            </div>
            <p className="font-medium text-sm leading-snug">{deptLabel(node, locale)}</p>
            {deptSubLabel(node, locale) !== deptLabel(node, locale) && (
              <p className="text-xs text-foreground/45 mt-0.5 leading-snug">{deptSubLabel(node, locale)}</p>
            )}
          </div>
          <div className="flex items-center gap-1 shrink-0 px-2 py-1 rounded-full bg-amber-500/10 text-amber-600 dark:text-amber-400 text-xs font-semibold">
            <Users size={11} />
            {node.totalUserCount}
          </div>
        </div>
      </div>
    </div>
  );
}

function DepartmentCard({
  node,
  index,
  locale,
  search,
  defaultOpen,
  sectionsLabel,
}: {
  node: DeptNode;
  index: number;
  locale: string;
  search: string;
  defaultOpen: boolean;
  sectionsLabel: string;
}) {
  const [open, setOpen] = useState(defaultOpen);
  const hasSections = node.children.length > 0;
  const sectionMatch = node.children.some((c) => matchesSearch(c, search));
  const selfMatch = matchesSearch(node, search);

  if (!selfMatch && !sectionMatch) return null;

  return (
    <div className="relative">
      {/* connector spine */}
      <div className="absolute left-[15px] top-0 bottom-0 w-px bg-gradient-to-b from-atg-blue/30 via-border/50 to-transparent" />

      <div className="relative flex gap-0 pl-0">
        {/* horizontal branch */}
        <div className="w-8 shrink-0 flex items-start pt-5">
          <div className="w-full h-px bg-gradient-to-r from-atg-blue/40 to-border/60" />
        </div>

        <div className="flex-1 pb-3 min-w-0">
          <div
            className={cn(
              "rounded-xl border bg-surface/80 backdrop-blur-sm overflow-hidden transition-all duration-200",
              "border-border/60 hover:border-atg-blue/30 hover:shadow-lg hover:shadow-atg-blue/5",
              hasSections && open && "ring-1 ring-atg-blue/15"
            )}
          >
            <div className="flex items-stretch">
              {hasSections && (
                <button
                  type="button"
                  onClick={() => setOpen((v) => !v)}
                  className="w-10 shrink-0 flex items-center justify-center border-r border-border/40 hover:bg-atg-blue/5 text-foreground/50 transition-colors"
                  aria-label={open ? "Collapse" : "Expand"}
                >
                  {open ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
                </button>
              )}
              <div className="flex-1 flex items-center gap-3 p-4 min-w-0">
                <div className="w-9 h-9 rounded-lg bg-atg-blue/15 flex items-center justify-center shrink-0">
                  <Layers size={17} className="text-atg-blue" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap mb-0.5">
                    <span className="text-[11px] font-bold text-atg-blue/70 tabular-nums tracking-wider">
                      {String(index).padStart(2, "0")}
                    </span>
                    <code className="text-[10px] font-mono px-1.5 py-0.5 rounded-md bg-border/40 text-foreground/60">
                      {node.code}
                    </code>
                  </div>
                  <p className="font-semibold text-[15px] leading-tight">{deptLabel(node, locale)}</p>
                  {deptSubLabel(node, locale) !== deptLabel(node, locale) && (
                    <p className="text-xs text-foreground/45 mt-0.5">{deptSubLabel(node, locale)}</p>
                  )}
                </div>
                <div className="shrink-0 text-right">
                  <div className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-atg-blue/10 text-atg-blue text-sm font-semibold">
                    <Users size={14} />
                    {node.totalUserCount}
                  </div>
                  {hasSections && node.userCount > 0 && (
                    <p className="text-[10px] text-foreground/40 mt-1 text-center">
                      {node.userCount} dept · {node.totalUserCount - node.userCount} sections
                    </p>
                  )}
                </div>
              </div>
            </div>

            {hasSections && open && (
              <div className="border-t border-border/40 bg-background/30 px-4 py-3">
                <p className="text-[10px] uppercase tracking-widest text-foreground/40 font-medium mb-2 pl-11">
                  {sectionsLabel}
                </p>
                <div className="pl-6 space-y-0">
                  {node.children.map((c, i) => (
                    <SectionCard key={c.id} node={c} index={i + 1} locale={locale} search={search} />
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function OrganizationBlock({
  node,
  locale,
  search,
  defaultOpen,
  sectionsLabel,
  isRoot,
}: {
  node: OrgNode;
  locale: string;
  search: string;
  defaultOpen: boolean;
  sectionsLabel: string;
  isRoot?: boolean;
}) {
  const [open, setOpen] = useState(defaultOpen);
  const style = ORG_STYLES[node.orgType] ?? ORG_STYLES.Station;
  const Icon = style.icon;

  const visibleDepts = node.departments.filter(
    (d) => matchesSearch(d, search) || d.children.some((c) => matchesSearch(c, search))
  );
  const visibleChildren = node.children.filter((c) => orgHasMatch(c, search));

  if (!isRoot && !orgHasMatch(node, search)) return null;

  return (
    <div className={cn(!isRoot && "ml-6 mt-4")}>
      <div
        className={cn(
          "rounded-2xl overflow-hidden border shadow-xl",
          style.ring,
          style.glow,
          isRoot ? "border-border/50" : "border-border/40"
        )}
      >
        {/* org header */}
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className={cn(
            "w-full flex items-center gap-4 px-5 py-4 text-left transition-opacity hover:opacity-95",
            "bg-gradient-to-r",
            style.accent
          )}
        >
          <div className="w-11 h-11 rounded-xl bg-white/15 backdrop-blur flex items-center justify-center shrink-0">
            <Icon size={22} className="text-white" />
          </div>
          <div className="flex-1 min-w-0 text-white">
            <p className="text-[10px] uppercase tracking-[0.2em] font-medium opacity-75 mb-0.5">{node.code}</p>
            <h2 className="text-lg font-bold leading-tight truncate">{node.name}</h2>
          </div>
          <div className="flex items-center gap-3 shrink-0">
            <div className="px-3 py-1.5 rounded-full bg-white/15 text-white text-sm font-bold flex items-center gap-1.5">
              <Users size={15} />
              {node.totalUserCount}
            </div>
            {open ? <ChevronDown size={20} className="text-white/70" /> : <ChevronRight size={20} className="text-white/70" />}
          </div>
        </button>

        {open && (
          <div className="bg-surface/50 p-5">
            {visibleDepts.length > 0 && (
              <div className="relative">
                {/* trunk line from org to departments */}
                <div className="absolute left-[15px] top-0 bottom-4 w-0.5 bg-gradient-to-b from-atg-blue/50 to-atg-blue/10 rounded-full" />
                <div className="space-y-1">
                  {node.departments
                    .filter(
                      (d) =>
                        matchesSearch(d, search) || d.children.some((c) => matchesSearch(c, search))
                    )
                    .map((d, i) => (
                      <DepartmentCard
                        key={d.id}
                        node={d}
                        index={i + 1}
                        locale={locale}
                        search={search}
                        defaultOpen={defaultOpen}
                        sectionsLabel={sectionsLabel}
                      />
                    ))}
                </div>
              </div>
            )}

            {visibleChildren.length > 0 && (
              <div className={cn(visibleDepts.length > 0 && "mt-6 pt-6 border-t border-border/40")}>
                {visibleChildren.map((c) => (
                  <OrganizationBlock
                    key={c.id}
                    node={c}
                    locale={locale}
                    search={search}
                    defaultOpen={defaultOpen}
                    sectionsLabel={sectionsLabel}
                  />
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function orgHasMatch(node: OrgNode, search: string): boolean {
  if (!search) return true;
  if (matchesSearch(node, search)) return true;
  if (node.departments.some((d) => matchesSearch(d, search) || d.children.some((c) => matchesSearch(c, search))))
    return true;
  return node.children.some((c) => orgHasMatch(c, search));
}

interface OrgTopologyViewProps {
  tree: OrgNode[];
  loading: boolean;
}

export function OrgTopologyView({ tree, loading }: OrgTopologyViewProps) {
  const t = useTranslations("admin");
  const locale = useLocale();
  const [expanded, setExpanded] = useState(true);
  const [search, setSearch] = useState("");

  const stats = useMemo(() => {
    const countOrgs = (nodes: OrgNode[]): number =>
      nodes.reduce((n, o) => n + 1 + countOrgs(o.children), 0);
    const countDepts = (nodes: DeptNode[]): number =>
      nodes.reduce((n, d) => n + 1 + countDepts(d.children), 0);
    const countDeptsInOrg = (o: OrgNode): number =>
      countDepts(o.departments) + o.children.reduce((n, c) => n + countDeptsInOrg(c), 0);
    return {
      orgCount: countOrgs(tree),
      deptCount: tree.reduce((n, o) => n + countDeptsInOrg(o), 0),
      userCount: tree.reduce((n, o) => n + o.totalUserCount, 0),
    };
  }, [tree]);

  return (
    <div className="space-y-6">
      {/* hero */}
      <div className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-br from-atg-blue/10 via-surface to-violet-500/5 p-6 sm:p-8">
        <div
          className="absolute inset-0 opacity-[0.35]"
          style={{
            backgroundImage: "radial-gradient(circle at 1px 1px, var(--border) 1px, transparent 0)",
            backgroundSize: "24px 24px",
          }}
        />
        <div className="relative flex flex-col lg:flex-row lg:items-end lg:justify-between gap-6">
          <div>
            <div className="inline-flex items-center gap-2 px-2.5 py-1 rounded-full bg-atg-blue/15 text-atg-blue text-xs font-semibold mb-3">
              <Network size={13} />
              {t("topologySubtitle")}
            </div>
            <h1 className="text-2xl sm:text-3xl font-bold tracking-tight">{t("hierarchy")}</h1>
            <p className="text-sm text-foreground/55 mt-2 max-w-xl">{t("hierarchyDesc")}</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={() => setExpanded((v) => !v)}
              className="text-sm px-4 py-2 rounded-lg border border-border/60 bg-surface/80 hover:bg-surface transition-colors font-medium"
            >
              {expanded ? t("collapseAll") : t("expandAll")}
            </button>
          </div>
        </div>

        <div className="relative grid grid-cols-3 gap-3 mt-6">
          {[
            { label: t("orgs"), value: stats.orgCount, color: "text-atg-blue" },
            { label: t("depts"), value: stats.deptCount, color: "text-atg-teal" },
            { label: t("totalUsers"), value: stats.userCount, color: "text-atg-purple" },
          ].map(({ label, value, color }) => (
            <div
              key={label}
              className="rounded-xl border border-white/10 bg-surface/60 backdrop-blur px-4 py-3 text-center sm:text-left"
            >
              <div className={cn("text-2xl font-bold tabular-nums", color)}>{value}</div>
              <div className="text-[11px] text-foreground/50 uppercase tracking-wider mt-0.5">{label}</div>
            </div>
          ))}
        </div>
      </div>

      {/* search */}
      <div className="relative">
        <Search size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-foreground/40" />
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t("topologySearch")}
          className="w-full h-11 pl-10 pr-10 rounded-xl border border-border bg-surface text-sm placeholder:text-foreground/40 focus:outline-none focus:ring-2 focus:ring-atg-blue/30 focus:border-atg-blue/40 transition-shadow"
        />
        {search && (
          <button
            type="button"
            onClick={() => setSearch("")}
            className="absolute right-3 top-1/2 -translate-y-1/2 p-1 rounded-md hover:bg-border/40 text-foreground/50"
          >
            <X size={14} />
          </button>
        )}
      </div>

      {/* legend */}
      <div className="flex flex-wrap gap-4 text-xs text-foreground/50 px-1">
        <span className="flex items-center gap-1.5">
          <span className="w-3 h-3 rounded-sm bg-gradient-to-br from-blue-600 to-indigo-700" />
          {t("legendHeadOffice")}
        </span>
        <span className="flex items-center gap-1.5">
          <span className="w-3 h-3 rounded-sm bg-gradient-to-br from-teal-600 to-cyan-700" />
          {t("legendBmgmc")}
        </span>
        <span className="flex items-center gap-1.5">
          <Layers size={12} className="text-atg-blue" />
          {t("legendDepartment")}
        </span>
        <span className="flex items-center gap-1.5">
          <span className="w-2 h-2 rounded-full bg-amber-500" />
          {t("legendSection")}
        </span>
      </div>

      {/* tree */}
      <div className="min-h-[400px]">
        {loading ? (
          <div className="flex flex-col items-center justify-center h-64 gap-3 text-foreground/40">
            <GitBranch size={32} className="animate-pulse" />
            <span className="text-sm">{t("loadingTopology")}</span>
          </div>
        ) : tree.length === 0 ? (
          <div className="flex items-center justify-center h-64 text-foreground/40 text-sm">{t("noTopology")}</div>
        ) : (
          <div className="space-y-8">
            {tree.map((n) => (
              <OrganizationBlock
                key={`${n.id}-${expanded}-${search}`}
                node={n}
                locale={locale}
                search={search}
                defaultOpen={expanded}
                sectionsLabel={t("sectionsLabel")}
                isRoot
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
