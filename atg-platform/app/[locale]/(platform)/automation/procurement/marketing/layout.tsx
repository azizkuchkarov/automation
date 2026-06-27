import { MarketingSidebar } from "@/components/dcs/MarketingSidebar";

export default function MarketingLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] bg-slate-50/80 dark:bg-background">
      <MarketingSidebar />
      <main className="flex-1 flex flex-col min-w-0 overflow-hidden">{children}</main>
    </div>
  );
}
