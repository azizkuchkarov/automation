import { DcsSidebar } from "@/components/dcs/DcsSidebar";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { cn } from "@/lib/utils";

export default function AutomationLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] bg-[#eef2f7] dark:bg-[#060a10]">
      <DcsSidebar />
      <main
        className={cn(
          "relative flex-1 flex flex-col min-w-0 overflow-hidden",
          dcsTheme.meshBg,
          dcsTheme.gridOverlay
        )}
      >
        <div className="relative flex-1 flex flex-col min-h-0">{children}</div>
      </main>
    </div>
  );
}
