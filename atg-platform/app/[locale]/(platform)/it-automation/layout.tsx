import { ItAutomationSidebar } from "@/components/it-automation/ItAutomationSidebar";

export default function ItAutomationLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] bg-[#eef4f7] dark:bg-[#060a10]">
      <ItAutomationSidebar />
      <main className="relative flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">{children}</main>
    </div>
  );
}
