/** Shared premium DCS surface classes (light theme) */
export const dcsTheme = {
  meshBg:
    "relative before:pointer-events-none before:absolute before:inset-0 before:bg-[radial-gradient(ellipse_80%_60%_at_50%_-20%,rgba(56,189,248,0.12),transparent),radial-gradient(ellipse_60%_40%_at_100%_0%,rgba(99,102,241,0.08),transparent),radial-gradient(ellipse_50%_30%_at_0%_100%,rgba(14,165,233,0.06),transparent)]",
  gridOverlay:
    "after:pointer-events-none after:absolute after:inset-0 after:bg-[linear-gradient(rgba(148,163,184,0.04)_1px,transparent_1px),linear-gradient(90deg,rgba(148,163,184,0.04)_1px,transparent_1px)] after:bg-[size:48px_48px]",
  glassPanel:
    "bg-white/75 backdrop-blur-xl border border-white/60 shadow-[0_4px_24px_-4px_rgba(15,23,42,0.08),0_0_0_1px_rgba(15,23,42,0.03)]",
  premiumCard:
    "rounded-2xl bg-white/80 backdrop-blur-md border border-slate-200/80 shadow-[0_1px_2px_rgba(15,23,42,0.04),0_12px_40px_-12px_rgba(15,23,42,0.12)]",
  tableShell:
    "rounded-2xl overflow-hidden bg-white/85 backdrop-blur-md border border-slate-200/70 shadow-[0_20px_50px_-24px_rgba(15,23,42,0.2)]",
  primaryBtn:
    "bg-gradient-to-r from-blue-600 to-sky-500 hover:from-blue-500 hover:to-sky-400 text-white shadow-lg shadow-blue-500/25 border-0",
} as const;
