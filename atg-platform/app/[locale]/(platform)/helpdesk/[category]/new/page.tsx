import { notFound } from "next/navigation";
import { CreateTicketForm } from "@/components/helpdesk/CreateTicketForm";
import { categoryFromSlug } from "@/lib/helpdesk";

export default async function HelpdeskCategoryCreatePage({
  params,
}: {
  params: Promise<{ locale: string; category: string }>;
}) {
  const { category: slug } = await params;
  const category = categoryFromSlug(slug);
  if (!category) notFound();
  return <CreateTicketForm fixedCategory={category} />;
}
