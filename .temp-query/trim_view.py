from pathlib import Path

p = Path(r"C:\Users\a.kuchkarov\Desktop\automation\atg-platform\components\dcs\ProcurementRequestView.tsx")
text = p.read_text(encoding="utf-8")
start = text.find("function OverviewTab(")
if start < 0:
    raise SystemExit("OverviewTab not found")

new_action = """function ActionCard({ title, subtitle, variant, children }: { title: string; subtitle?: string; variant: "amber" | "violet" | "sky"; children: React.ReactNode }) {
  const styles = {
    amber: "border-amber-500/30 bg-amber-500/5",
    violet: "border-violet-500/30 bg-violet-500/5",
    sky: "border-sky-500/30 bg-sky-500/5",
  };
  return (
    <div className={cn("rounded-2xl border p-5", styles[variant])}>
      <h2 className="text-sm font-bold mb-1">{title}</h2>
      {subtitle && <p className="text-sm text-foreground/55 mb-4">{subtitle}</p>}
      {children}
    </div>
  );
}
"""
p.write_text(text[:start] + new_action, encoding="utf-8")
print("trimmed ok")
