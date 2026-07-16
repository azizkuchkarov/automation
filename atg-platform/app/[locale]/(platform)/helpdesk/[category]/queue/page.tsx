import { notFound, redirect } from "next/navigation";
import { categoryFromSlug } from "@/lib/helpdesk";

export default async function HelpdeskCategoryQueuePage({
  params,
}: {
  params: Promise<{ locale: string; category: string }>;
}) {
  const { locale, category: slug } = await params;
  const category = categoryFromSlug(slug);
  if (!category) notFound();
  redirect(`/${locale}/helpdesk/${slug}/tickets?view=queue`);
}
