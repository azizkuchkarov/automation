import { Suspense } from "react";
import { HelpdeskSidebar } from "@/components/helpdesk/HelpdeskSidebar";

function SidebarFallback() {
  return <aside className="w-[252px] shrink-0 border-r border-border/70 bg-surface" />;
}

export default function HelpdeskLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] bg-background">
      <Suspense fallback={<SidebarFallback />}>
        <HelpdeskSidebar />
      </Suspense>
      <main className="flex min-w-0 flex-1 flex-col overflow-hidden">{children}</main>
    </div>
  );
}
