import { HrSidebar } from "@/components/hr/HrSidebar";

export default function HrLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] bg-slate-50/80">
      <HrSidebar />
      <main className="flex-1 flex flex-col min-w-0 overflow-hidden">{children}</main>
    </div>
  );
}
