"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import api from "@/lib/api";

interface Position {
  id: string;
  name: string;
  code: string;
}

export default function PositionsPage() {
  const t = useTranslations("admin");
  const [positions, setPositions] = useState<Position[]>([]);

  useEffect(() => {
    api.get("/positions").then((r) => setPositions(r.data));
  }, []);

  return (
    <div>
      <h1 className="text-2xl font-semibold mb-6">{t("positions")}</h1>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-surface border-b border-border">
            <tr>
              <th className="text-left p-3">Name</th>
              <th className="text-left p-3">Code</th>
            </tr>
          </thead>
          <tbody>
            {positions.map((p) => (
              <tr key={p.id} className="border-b border-border/50 h-10">
                <td className="p-3">{p.name}</td>
                <td className="p-3">{p.code}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
