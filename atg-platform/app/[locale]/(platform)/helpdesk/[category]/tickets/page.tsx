import { Suspense } from "react";
import { notFound } from "next/navigation";
import { CategoryTicketsPage } from "@/components/helpdesk/CategoryTicketsPage";
import { categoryFromSlug } from "@/lib/helpdesk";

function TicketsFallback() {
  return <div className="flex flex-1 items-center justify-center text-sm text-foreground/40">Loading…</div>;
}

export default async function HelpdeskCategoryTicketsPage({
  params,
}: {
  params: Promise<{ locale: string; category: string }>;
}) {
  const { category: slug } = await params;
  const category = categoryFromSlug(slug);
  if (!category) notFound();

  return (
    <Suspense fallback={<TicketsFallback />}>
      <CategoryTicketsPage category={category} />
    </Suspense>
  );
}
