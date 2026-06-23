import { TopBar } from "@/components/layout/TopBar";
import { AdminSidebar } from "@/components/layout/AdminSidebar";
import { AuthGuard } from "@/components/layout/AuthGuard";

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <AuthGuard adminOnly>
      <div className="min-h-screen flex flex-col">
        <TopBar />
        <div className="flex flex-1">
          <AdminSidebar />
          <main className="flex-1 p-6 overflow-auto">{children}</main>
        </div>
      </div>
    </AuthGuard>
  );
}
