"use client";

import { useEffect, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import {
  ArrowRight,
  Briefcase,
  Building2,
  Calculator,
  ChevronDown,
  ChevronRight,
  Cpu,
  Crown,
  FileSignature,
  FlaskConical,
  Gauge,
  Layers,
  Network,
  Plane,
  Rocket,
  Scale,
  Search,
  ShieldCheck,
  ShieldQuestion,
  Sparkles,
  UserCog,
  Users,
  Wallet,
  X,
} from "lucide-react";
import {
  fetchHrBusinessTripWorkflowAdmin,
  type HrBusinessTripDeptWorkflow,
  type HrBusinessTripWorkflowStep,
  type HrBusinessTripWorkflowTier,
} from "@/lib/hrBusinessTripWorkflow";
import { getApiErrorMessage } from "@/lib/api";
import { cn } from "@/lib/utils";

const DEPT_ICONS: Record<string, typeof Users> = {
  "HO-HR": Users,
  "HO-SEC": ShieldCheck,
  "HO-AC": ShieldQuestion,
  "HO-FINPLAN": Wallet,
  "HO-ACCT": Calculator,
  "HO-ENGCON": Layers,
  "HO-NEWPRJ": Rocket,
  "HO-ITDIG": Cpu,
  "HO-MKT": Sparkles,
  "HO-CPROC": FileSignature,
  "HO-DCPR": Briefcase,
  "HO-QHSE": FlaskConical,
  "HO-GASM": Gauge,
  "HO-LEGAL": Scale,
  "HO-ADM": Building2,
  "HO-EXEC": Crown,
};

const DEPT_ACCENTS: Record<string, string> = {
  "HO-HR": "from-blue-500/15 text-blue-600",
  "HO-SEC": "from-red-500/15 text-red-600",
  "HO-AC": "from-rose-500/15 text-rose-600",
  "HO-FINPLAN": "from-emerald-500/15 text-emerald-600",
  "HO-ACCT": "from-green-500/15 text-green-600",
  "HO-ENGCON": "from-amber-500/15 text-amber-600",
  "HO-NEWPRJ": "from-orange-500/15 text-orange-600",
  "HO-ITDIG": "from-cyan-500/15 text-cyan-600",
  "HO-MKT": "from-fuchsia-500/15 text-fuchsia-600",
  "HO-CPROC": "from-indigo-500/15 text-indigo-600",
  "HO-DCPR": "from-sky-500/15 text-sky-600",
  "HO-QHSE": "from-teal-500/15 text-teal-600",
  "HO-GASM": "from-lime-500/15 text-lime-600",
  "HO-LEGAL": "from-violet-500/15 text-violet-600",
  "HO-ADM": "from-slate-500/15 text-slate-600",
  "HO-EXEC": "from-yellow-500/15 text-yellow-600",
};

const ROLE_META: Record<
  string,
  { badge: string; dot: string; ring: string }
> = {
  GeneralDirector: {
    badge: "bg-yellow-500/15 text-yellow-700 border-yellow-500/30",
    dot: "bg-yellow-500",
    ring: "ring-yellow-500/25",
  },
  FirstDeputyGeneralDirector: {
    badge: "bg-violet-500/15 text-violet-700 border-violet-500/30",
    dot: "bg-violet-500",
    ring: "ring-violet-500/25",
  },
  DeputyGeneralDirector: {
    badge: "bg-purple-500/15 text-purple-700 border-purple-500/30",
    dot: "bg-purple-500",
    ring: "ring-purple-500/25",
  },
  HrManager: {
    badge: "bg-sky-500/15 text-sky-700 border-sky-500/30",
    dot: "bg-sky-500",
    ring: "ring-sky-500/25",
  },
  DepartmentHead: {
    badge: "bg-atg-blue/15 text-atg-blue border-atg-blue/30",
    dot: "bg-atg-blue",
    ring: "ring-atg-blue/25",
  },
  DeputyDepartmentHead: {
    badge: "bg-teal-500/15 text-teal-700 border-teal-500/30",
    dot: "bg-teal-500",
    ring: "ring-teal-500/25",
  },
};

const DEFAULT_ROLE_META = {
  badge: "bg-border/50 text-foreground/60 border-border",
  dot: "bg-foreground/40",
  ring: "ring-border",
};

function roleMeta(role: string) {
  return ROLE_META[role] ?? DEFAULT_ROLE_META;
}

function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[1][0]).toUpperCase();
}

export default function AdminHrBusinessTripWorkflowPage() {
  const t = useTranslations("admin.hrBusinessTripWorkflow");
  const locale = useLocale();
  const isRu = locale === "ru";

  const [departments, setDepartments] = useState<HrBusinessTripDeptWorkflow[]>([]);
  const [orgName, setOrgName] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [openDept, setOpenDept] = useState<string | null>(null);
  const [search, setSearch] = useState("");

  useEffect(() => {
    setLoading(true);
    fetchHrBusinessTripWorkflowAdmin()
      .then((data) => {
        setDepartments(data.departments);
        setOrgName(data.organizationName ?? null);
      })
      .catch((err) => setError(getApiErrorMessage(err, t("loadError"))))
      .finally(() => setLoading(false));
  }, [t]);

  const tierTitle = (tier: HrBusinessTripWorkflowTier) => (isRu ? tier.titleRu : tier.titleEn);
  const deptTitle = (d: HrBusinessTripDeptWorkflow) => (isRu ? d.titleRu : d.titleEn);

  const stats = useMemo(() => {
    let tiers = 0;
    const approvers = new Set<string>();
    for (const d of departments) {
      tiers += d.tiers.length;
      for (const tier of d.tiers) for (const s of tier.steps) approvers.add(s.approverUserId);
    }
    return { depts: departments.length, tiers, approvers: approvers.size };
  }, [departments]);

  const filtered = useMemo(() => {
    if (!search.trim()) return departments;
    const q = search.toLowerCase();
    return departments.filter((d) => {
      if (
        d.titleRu.toLowerCase().includes(q) ||
        d.titleEn.toLowerCase().includes(q) ||
        d.departmentCode.toLowerCase().includes(q)
      )
        return true;
      return d.tiers.some((tier) =>
        tier.steps.some((s) => s.approverName.toLowerCase().includes(q)) ||
        tier.initiators.some((p) => p.fullName.toLowerCase().includes(q)),
      );
    });
  }, [departments, search]);

  return (
    <div className="space-y-6">
      {/* hero */}
      <div className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-br from-violet-500/10 via-surface to-atg-blue/5 p-6 sm:p-8">
        <div
          className="absolute inset-0 opacity-[0.35]"
          style={{
            backgroundImage: "radial-gradient(circle at 1px 1px, var(--border) 1px, transparent 0)",
            backgroundSize: "24px 24px",
          }}
        />
        <div className="relative flex flex-col lg:flex-row lg:items-end lg:justify-between gap-6">
          <div className="min-w-0">
            <div className="inline-flex items-center gap-2 px-2.5 py-1 rounded-full bg-violet-500/15 text-violet-700 text-xs font-semibold mb-3">
              <Plane size={13} />
              {orgName ?? t("headOffice")}
            </div>
            <h1 className="text-2xl sm:text-3xl font-bold tracking-tight">{t("title")}</h1>
            <p className="text-sm text-foreground/55 mt-2 max-w-2xl">{t("subtitle")}</p>
          </div>
        </div>

        <div className="relative grid grid-cols-3 gap-3 mt-6">
          {[
            { label: t("statDepartments"), value: stats.depts, color: "text-violet-600", tint: "bg-violet-500/10", icon: Building2 },
            { label: t("statTiers"), value: stats.tiers, color: "text-atg-blue", tint: "bg-atg-blue/10", icon: Layers },
            { label: t("statApprovers"), value: stats.approvers, color: "text-emerald-600", tint: "bg-emerald-500/10", icon: UserCog },
          ].map(({ label, value, color, tint, icon: Icon }) => (
            <div
              key={label}
              className="rounded-xl border border-white/10 bg-surface/60 backdrop-blur px-4 py-3 flex items-center gap-3"
            >
              <div className={cn("rounded-lg p-2", tint)}>
                <Icon size={18} className={color} />
              </div>
              <div>
                <div className={cn("text-2xl font-bold tabular-nums leading-none", color)}>{value}</div>
                <div className="text-[11px] text-foreground/50 uppercase tracking-wider mt-1">{label}</div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* toolbar */}
      <div className="flex flex-col sm:flex-row gap-3 sm:items-center">
        <div className="relative flex-1">
          <Search size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-foreground/40" />
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t("searchPlaceholder")}
            className="w-full h-11 pl-10 pr-10 rounded-xl border border-border bg-surface text-sm placeholder:text-foreground/40 focus:outline-none focus:ring-2 focus:ring-violet-500/30 focus:border-violet-500/40 transition-shadow"
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
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => setOpenDept("__all__")}
            className="text-sm px-4 py-2 rounded-lg border border-border/60 bg-surface/80 hover:bg-surface transition-colors font-medium"
          >
            {t("expandAll")}
          </button>
          <button
            type="button"
            onClick={() => setOpenDept(null)}
            className="text-sm px-4 py-2 rounded-lg border border-border/60 bg-surface/80 hover:bg-surface transition-colors font-medium"
          >
            {t("collapseAll")}
          </button>
        </div>
      </div>

      {error && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {/* content */}
      {loading ? (
        <div className="flex flex-col items-center justify-center h-64 gap-3 text-foreground/40">
          <Plane size={32} className="animate-pulse" />
          <span className="text-sm">{t("loading")}</span>
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center h-64 gap-2 text-foreground/40">
          <Network size={28} />
          <span className="text-sm">{search ? t("noResults") : t("empty")}</span>
        </div>
      ) : (
        <div className="grid gap-4">
          {filtered.map((dept) => (
            <DepartmentCard
              key={dept.id}
              dept={dept}
              open={openDept === dept.departmentCode || openDept === "__all__"}
              onToggle={() =>
                setOpenDept((prev) =>
                  prev === dept.departmentCode ? null : dept.departmentCode,
                )
              }
              deptTitle={deptTitle(dept)}
              tierTitle={tierTitle}
              t={t}
            />
          ))}
        </div>
      )}

      {/* legend */}
      {!loading && filtered.length > 0 && (
        <div className="flex flex-wrap gap-x-5 gap-y-2 text-xs text-foreground/50 px-1 pt-2">
          {[
            { role: "DepartmentHead", label: t("roleDepartmentHead") },
            { role: "DeputyDepartmentHead", label: t("roleDeputyDepartmentHead") },
            { role: "HrManager", label: t("roleHrManager") },
            { role: "FirstDeputyGeneralDirector", label: t("roleFdgd") },
            { role: "GeneralDirector", label: `${t("roleGd")} · E-IMZO` },
          ].map(({ role, label }) => (
            <span key={role} className="flex items-center gap-1.5">
              <span className={cn("w-2.5 h-2.5 rounded-full", roleMeta(role).dot)} />
              {label}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

function DepartmentCard({
  dept,
  open,
  onToggle,
  deptTitle,
  tierTitle,
  t,
}: {
  dept: HrBusinessTripDeptWorkflow;
  open: boolean;
  onToggle: () => void;
  deptTitle: string;
  tierTitle: (tier: HrBusinessTripWorkflowTier) => string;
  t: ReturnType<typeof useTranslations>;
}) {
  const Icon = DEPT_ICONS[dept.departmentCode] ?? Building2;
  const accent = DEPT_ACCENTS[dept.departmentCode] ?? "from-slate-500/15 text-slate-600";
  const stepCount = dept.tiers.reduce((n, tier) => n + tier.steps.length, 0);

  return (
    <div
      className={cn(
        "rounded-2xl border bg-surface overflow-hidden transition-all duration-200",
        open
          ? "border-violet-500/30 shadow-lg shadow-violet-500/5"
          : "border-border hover:border-violet-500/25 hover:shadow-md",
      )}
    >
      <button
        type="button"
        onClick={onToggle}
        className="flex w-full items-center gap-4 px-4 sm:px-5 py-4 text-left"
      >
        <div className={cn("flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br to-surface", accent)}>
          <Icon size={20} />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2 mb-0.5">
            <code className="text-[10px] font-mono px-1.5 py-0.5 rounded-md bg-border/40 text-foreground/60">
              {dept.departmentCode}
            </code>
          </div>
          <h2 className="font-semibold text-[15px] leading-tight truncate">{deptTitle}</h2>
        </div>
        <div className="hidden sm:flex items-center gap-2 shrink-0">
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-atg-blue/10 text-atg-blue text-xs font-semibold">
            <Layers size={12} />
            {t("tierCount", { count: dept.tiers.length })}
          </span>
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-emerald-500/10 text-emerald-600 text-xs font-semibold">
            <UserCog size={12} />
            {stepCount}
          </span>
        </div>
        <div className="shrink-0 text-foreground/40">
          {open ? <ChevronDown size={18} /> : <ChevronRight size={18} />}
        </div>
      </button>

      {open && (
        <div className="border-t border-border/60 bg-background/30 p-4 sm:p-5 space-y-3">
          {dept.tiers.map((tier) => (
            <TierRow key={tier.id} tier={tier} title={tierTitle(tier)} t={t} />
          ))}
        </div>
      )}
    </div>
  );
}

function TierRow({
  tier,
  title,
  t,
}: {
  tier: HrBusinessTripWorkflowTier;
  title: string;
  t: ReturnType<typeof useTranslations>;
}) {
  return (
    <div className="rounded-xl border border-border/60 bg-surface p-4">
      <div className="flex flex-wrap items-center gap-2 mb-3">
        <span className="text-sm font-semibold">{title}</span>
        {tier.catchAllStaff && (
          <span className="text-[11px] rounded-full bg-amber-500/15 text-amber-700 px-2 py-0.5 font-medium">
            {t("catchAllStaff")}
          </span>
        )}
        {tier.prependsSectionManager && (
          <span className="text-[11px] rounded-full bg-sky-500/15 text-sky-700 px-2 py-0.5 font-medium">
            {t("sectionManagerPrefix")}
          </span>
        )}
      </div>

      {/* initiators */}
      <div className="mb-3">
        <p className="text-[11px] uppercase tracking-wider text-foreground/40 font-medium mb-1.5">
          {t("initiators")}
        </p>
        {tier.initiators.length === 0 ? (
          <p className="text-xs text-foreground/45 italic">{t("initiatorsAll")}</p>
        ) : (
          <div className="flex flex-wrap gap-1.5">
            {tier.initiators.map((p) => (
              <span
                key={p.userId}
                className="inline-flex items-center gap-1.5 text-xs rounded-full border border-border bg-background pl-1 pr-2.5 py-0.5"
                title={p.email}
              >
                <span className="flex h-5 w-5 items-center justify-center rounded-full bg-foreground/10 text-[9px] font-bold text-foreground/60">
                  {initials(p.fullName)}
                </span>
                {p.fullName}
              </span>
            ))}
          </div>
        )}
      </div>

      {/* approval chain */}
      <div>
        <p className="text-[11px] uppercase tracking-wider text-foreground/40 font-medium mb-2">
          {t("approvalChain")}
        </p>
        {tier.steps.length === 0 ? (
          <p className="text-xs text-foreground/45 italic">{t("noApprovers")}</p>
        ) : (
          <div className="flex flex-wrap items-stretch gap-y-2">
            {tier.steps.map((step, idx) => (
              <div key={step.id} className="flex items-center">
                <StepChip step={step} index={idx + 1} t={t} />
                {idx < tier.steps.length - 1 && (
                  <ArrowRight size={16} className="mx-1.5 text-foreground/30 shrink-0" />
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function StepChip({
  step,
  index,
  t,
}: {
  step: HrBusinessTripWorkflowStep;
  index: number;
  t: ReturnType<typeof useTranslations>;
}) {
  const meta = roleMeta(step.role);
  const isGd = step.role === "GeneralDirector";
  return (
    <div
      className={cn(
        "relative flex items-center gap-2 rounded-xl border bg-background px-2.5 py-1.5 ring-1 ring-inset",
        meta.ring,
        "border-border",
      )}
    >
      <span className="flex h-6 w-6 items-center justify-center rounded-full bg-foreground/[0.06] text-[10px] font-bold tabular-nums text-foreground/50">
        {index}
      </span>
      <div className="min-w-0">
        <div className="flex items-center gap-1.5">
          <span className={cn("w-1.5 h-1.5 rounded-full", meta.dot)} />
          <span className="text-[13px] font-medium leading-tight truncate max-w-[160px]">
            {step.approverName}
          </span>
          {isGd && (
            <span className="inline-flex items-center gap-0.5 text-[9px] font-bold uppercase tracking-wide rounded bg-yellow-500/20 text-yellow-700 px-1 py-0.5">
              E-IMZO
            </span>
          )}
        </div>
        <span className={cn("inline-block mt-0.5 text-[10px] rounded border px-1 py-px", meta.badge)}>
          {t(`role_${step.role}` as "role_GeneralDirector")}
        </span>
      </div>
    </div>
  );
}
