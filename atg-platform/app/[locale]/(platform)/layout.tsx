import { TopBar } from "@/components/layout/TopBar";
import { AuthGuard } from "@/components/layout/AuthGuard";

export default function PlatformLayout({ children }: { children: React.ReactNode }) {
  return (
    <AuthGuard>
      <div className="min-h-screen flex flex-col">
        <TopBar />
        <main className="flex-1 bg-background">{children}</main>
      </div>
    </AuthGuard>
  );
}
