"use client";

import { useEffect, useState } from "react";
import api from "@/lib/api";
import { OrgNode, OrgTopologyView } from "@/components/admin/OrgTopologyView";

export default function HierarchyPage() {
  const [tree, setTree] = useState<OrgNode[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get("/organizations/hierarchy")
      .then((r) => setTree(r.data))
      .finally(() => setLoading(false));
  }, []);

  return <OrgTopologyView tree={tree} loading={loading} />;
}
