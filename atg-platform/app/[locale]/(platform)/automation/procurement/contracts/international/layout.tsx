import { ContractsSectionNav } from "@/components/dcs/ContractsSectionNav";

export default function ContractsInternationalLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] flex-col bg-slate-50/80 dark:bg-background">
      <ContractsSectionNav section="International" />
      <main className="flex min-w-0 flex-1 flex-col overflow-hidden">{children}</main>
    </div>
  );
}
