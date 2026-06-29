"use client";

import {
  ArrowRight,
  Building2,
  CheckCircle2,
  Circle,
  Clock,
  FileText,
  Megaphone,
  Scale,
  Wrench,
} from "lucide-react";
import {
  ProcurementTopologyNode,
  topologyDept,
  topologyLabel,
  topologyStatusLabel,
} from "@/lib/procurementRequest";
import { cn } from "@/lib/utils";

const NODE_ICONS: Record<string, typeof Wrench> = {
  initiation: FileText,
  tas_workflow: Wrench,
  approval: Scale,
  marketing: Megaphone,
  contracts: Building2,
};

interface Props {
  nodes: ProcurementTopologyNode[];
  locale: string;
  hint?: string;
}

export function ProcurementTopologyView({ nodes, locale, hint }: Props) {
  const visible = nodes.filter((n) => n.status !== "Skipped");
  const activeIndex = visible.findIndex((n) => n.status === "Active");
  const completedCount = visible.filter((n) => n.status === "Completed").length;

  return (
    <div className="space-y-6">
      {hint && <p className="text-sm text-foreground/55 max-w-3xl leading-relaxed">{hint}</p>}

      <div className="rounded-2xl border border-border/70 bg-surface shadow-sm overflow-hidden">
        <div className="px-5 py-4 border-b border-border/50 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-bold">
              {locale.startsWith("en") ? "Process topology" : "Топология процесса"}
            </h2>
            <p className="text-xs text-foreground/45 mt-0.5">
              {locale.startsWith("en")
                ? "End-to-end routing from initiation to contracts"
                : "Маршрут от инициации до контрактов"}
            </p>
          </div>
          <span className="text-xs font-semibold px-3 py-1 rounded-full bg-foreground/[0.06]">
            {completedCount}/{visible.length}{" "}
            {locale.startsWith("en") ? "completed" : "завершено"}
          </span>
        </div>

        <div className="p-6 overflow-x-auto">
          <div className="flex items-stretch min-w-max gap-0">
            {visible.map((node, i) => {
              const Icon = NODE_ICONS[node.key] ?? Circle;
              const isActive = node.status === "Active";
              const isDone = node.status === "Completed";
              const isPast = i < activeIndex || (activeIndex === -1 && isDone);

              return (
                <div key={node.key} className="flex items-center">
                  <div
                    className={cn(
                      "relative w-52 rounded-2xl border p-4 transition-all duration-300",
                      isDone && "border-emerald-500/35 bg-emerald-500/[0.06]",
                      isActive && "border-sky-500/45 bg-sky-500/[0.08] shadow-lg shadow-sky-500/10 scale-[1.02]",
                      !isDone && !isActive && "border-border/60 bg-foreground/[0.02] opacity-75"
                    )}
                  >
                    {isActive && (
                      <span className="absolute -top-2.5 left-4 text-[9px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full bg-sky-500 text-white">
                        {locale.startsWith("en") ? "In progress" : "В работе"}
                      </span>
                    )}
                    <div className="flex items-center gap-2 mb-3">
                      <div
                        className={cn(
                          "w-9 h-9 rounded-xl flex items-center justify-center",
                          isDone && "bg-emerald-500/15 text-emerald-600",
                          isActive && "bg-sky-500/15 text-sky-600",
                          !isDone && !isActive && "bg-foreground/[0.06] text-foreground/35"
                        )}
                      >
                        {isDone ? <CheckCircle2 size={18} /> : isActive ? <Clock size={18} /> : <Icon size={18} />}
                      </div>
                      <span
                        className={cn(
                          "text-[10px] font-bold uppercase tracking-wider",
                          isActive ? "text-sky-600" : "text-foreground/40"
                        )}
                      >
                        {topologyStatusLabel(node.status, locale)}
                      </span>
                    </div>
                    <p className="text-sm font-semibold leading-snug">{topologyLabel(node, locale)}</p>
                    {topologyDept(node, locale) && (
                      <p className="text-[11px] text-foreground/45 mt-2 leading-tight">
                        {topologyDept(node, locale)}
                      </p>
                    )}
                    {node.assigneeName && (
                      <p className="text-xs text-foreground/60 mt-2 truncate">{node.assigneeName}</p>
                    )}
                    {node.completedAt && (
                      <p className="text-[10px] text-foreground/35 mt-2">
                        {new Date(node.completedAt).toLocaleString(locale)}
                      </p>
                    )}
                  </div>
                  {i < visible.length - 1 && (
                    <div className="flex items-center px-2">
                      <div
                        className={cn(
                          "w-8 h-0.5 rounded-full",
                          isPast || isDone ? "bg-emerald-400" : "bg-border"
                        )}
                      />
                      <ArrowRight
                        size={14}
                        className={cn(
                          "mx-0.5 shrink-0",
                          isPast || isDone ? "text-emerald-500" : "text-foreground/20"
                        )}
                      />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      </div>

      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {visible.map((node) => (
          <TopologyDetailCard key={node.key} node={node} locale={locale} />
        ))}
      </div>
    </div>
  );
}

function TopologyDetailCard({ node, locale }: { node: ProcurementTopologyNode; locale: string }) {
  return (
    <div
      className={cn(
        "rounded-xl border p-4 bg-surface",
        node.status === "Active" && "border-sky-500/40 ring-2 ring-sky-500/10",
        node.status === "Completed" && "border-emerald-500/25"
      )}
    >
      <div className="flex items-center justify-between gap-2 mb-2">
        <p className="text-sm font-semibold">{topologyLabel(node, locale)}</p>
        <span className="text-[10px] font-bold uppercase text-foreground/45">
          {topologyStatusLabel(node.status, locale)}
        </span>
      </div>
      {topologyDept(node, locale) && (
        <p className="text-xs text-foreground/50">{topologyDept(node, locale)}</p>
      )}
      {node.assigneeName && <p className="text-xs text-foreground/60 mt-2">{node.assigneeName}</p>}
      {node.completedAt && (
        <p className="text-[10px] text-foreground/40 mt-2">
          {new Date(node.completedAt).toLocaleString(locale)}
        </p>
      )}
    </div>
  );
}
