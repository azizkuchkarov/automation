import { notFound } from "next/navigation";
import { CategoryBoardPage } from "@/components/helpdesk/CategoryBoardPage";
import { categoryFromSlug } from "@/lib/helpdesk";

export default async function HelpdeskCategoryBoardPage({
  params,
}: {
  params: Promise<{ locale: string; category: string }>;
}) {
  const { category: slug } = await params;
  const category = categoryFromSlug(slug);
  if (!category) notFound();
  return <CategoryBoardPage category={category} />;
}
