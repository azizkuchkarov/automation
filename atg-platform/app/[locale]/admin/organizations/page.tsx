"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import api from "@/lib/api";
import { Badge } from "@/components/ui/Badge";

interface OrgNode {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  userCount: number;
  children: OrgNode[];
}

function OrgTreeNode({ node, depth = 0 }: { node: OrgNode; depth?: number }) {
  return (
    <div>
      <div className="flex items-center gap-2 py-2 border-b border-border/30" style={{ paddingLeft: depth * 20 }}>
        <Badge className="bg-atg-blue/20 text-atg-blue">{node.code}</Badge>
        <span className="font-medium">{node.name}</span>
        <span className="text-xs text-foreground/50">{node.userCount} users</span>
        <Badge className={node.isActive ? "bg-green-500/20 text-green-400" : "bg-red-500/20 text-red-400"}>
          {node.isActive ? "Active" : "Inactive"}
        </Badge>
      </div>
      {node.children?.map((c) => <OrgTreeNode key={c.id} node={c} depth={depth + 1} />)}
    </div>
  );
}

export default function OrganizationsPage() {
  const t = useTranslations("admin");
  const [tree, setTree] = useState<OrgNode[]>([]);

  useEffect(() => {
    api.get("/organizations").then((r) => setTree(r.data));
  }, []);

  return (
    <div>
      <h1 className="text-2xl font-semibold mb-6">{t("organizations")}</h1>
      <div className="rounded-lg border border-border bg-surface p-4">
        {tree.map((n) => <OrgTreeNode key={n.id} node={n} />)}
      </div>
    </div>
  );
}
