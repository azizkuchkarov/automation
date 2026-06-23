import Link from "next/link";
import { LucideIcon } from "lucide-react";
import { ArrowRight } from "lucide-react";

interface ModuleCardProps {
  href: string;
  icon: LucideIcon;
  title: string;
  description: string;
  color: string;
}

export function ModuleCard({ href, icon: Icon, title, description, color }: ModuleCardProps) {
  return (
    <Link
      href={href}
      className="group relative flex flex-col gap-3 rounded-xl border border-border bg-surface p-6 transition-all hover:border-transparent hover:shadow-lg"
      style={{ ["--accent" as string]: color }}
    >
      <div className="flex items-start justify-between">
        <div className="rounded-lg p-3" style={{ backgroundColor: `${color}20`, color }}>
          <Icon size={32} />
        </div>
        <ArrowRight size={18} className="opacity-0 group-hover:opacity-100 transition-opacity text-foreground/50" />
      </div>
      <div>
        <h3 className="text-lg font-medium">{title}</h3>
        <p className="text-sm text-foreground/60 mt-1">{description}</p>
      </div>
    </Link>
  );
}
