import { HrSidebar } from "@/components/hr/HrSidebar";

export default function HrLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] bg-[linear-gradient(180deg,#f8fafc_0%,#f1f5f9_100%)]">
      <HrSidebar />
      <main className="flex-1 flex flex-col min-w-0 overflow-hidden">{children}</main>
    </div>
  );
}
