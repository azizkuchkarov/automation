import api from "@/lib/api";
import {
  HelpDeskCategory,
  TicketBoard,
  TicketCategory,
  TicketListItem,
  categoryFromSlug,
} from "@/lib/helpdesk";

export async function fetchHelpDeskCategories(): Promise<HelpDeskCategory[]> {
  const { data } = await api.get<HelpDeskCategory[]>("/helpdesk/categories");
  return data;
}

export async function fetchHelpDeskBoard(category?: TicketCategory): Promise<TicketBoard> {
  const params = category ? `?category=${category}` : "";
  const { data } = await api.get<TicketBoard>(`/helpdesk/board${params}`);
  return data;
}

export async function fetchHelpDeskTickets(
  view: string,
  category?: TicketCategory,
  pageSize = 100,
): Promise<TicketListItem[]> {
  const params = new URLSearchParams({ view, pageSize: String(pageSize) });
  if (category) params.set("category", category);
  const { data } = await api.get<{ items: TicketListItem[] }>(`/helpdesk/tickets?${params}`);
  return data.items ?? [];
}

export function parseCategorySlug(slug: string | undefined): TicketCategory | null {
  if (!slug) return null;
  return categoryFromSlug(slug);
}
