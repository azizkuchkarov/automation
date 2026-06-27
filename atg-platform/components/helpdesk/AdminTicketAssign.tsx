"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import api from "@/lib/api";
import { HelpDeskAssignee, TicketListItem, TicketStatus } from "@/lib/helpdesk";
import { Button } from "@/components/ui/Button";
import { UserPlus } from "lucide-react";

const ASSIGNABLE: TicketStatus[] = ["Open", "Assigned"];

export function AdminTicketAssign({ ticket, onAssigned }: { ticket: TicketListItem; onAssigned: () => void }) {
  const t = useTranslations("helpdesk");
  const [open, setOpen] = useState(false);
  const [assignees, setAssignees] = useState<HelpDeskAssignee[]>([]);
  const [selected, setSelected] = useState("");
  const [loading, setLoading] = useState(false);

  const canAssign = ASSIGNABLE.includes(ticket.status);

  useEffect(() => {
    if (open && canAssign) {
      api.get(`/helpdesk/tickets/${ticket.id}/assignees`).then((r) => setAssignees(r.data));
    }
  }, [open, ticket.id, canAssign]);

  if (!canAssign) return <span className="text-foreground/30">—</span>;

  const assign = async () => {
    if (!selected) return;
    setLoading(true);
    try {
      await api.post(`/helpdesk/tickets/${ticket.id}/assign`, { assigneeId: selected });
      setOpen(false);
      onAssigned();
    } finally {
      setLoading(false);
    }
  };

  if (!open) {
    return (
      <Button size="sm" variant="secondary" onClick={() => setOpen(true)}>
        <UserPlus size={12} className="mr-1" />
        {t("admin.assign")}
      </Button>
    );
  }

  return (
    <div className="flex items-center gap-1.5 min-w-[220px]">
      <select
        value={selected}
        onChange={(e) => setSelected(e.target.value)}
        className="flex-1 h-8 rounded border border-border bg-surface px-2 text-xs"
      >
        <option value="">{t("actions.selectAssignee")}</option>
        {assignees.map((a) => (
          <option key={a.id} value={a.id}>{a.fullName}</option>
        ))}
      </select>
      <Button size="sm" disabled={!selected || loading} onClick={assign}>
        OK
      </Button>
      <button type="button" onClick={() => setOpen(false)} className="text-xs text-foreground/40 hover:text-foreground">
        ✕
      </button>
    </div>
  );
}
