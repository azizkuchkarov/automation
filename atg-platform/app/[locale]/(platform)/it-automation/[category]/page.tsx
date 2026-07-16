"use client";

import { useParams } from "next/navigation";
import { ItAssetCategoryPage } from "@/components/it-automation/ItAssetCategoryPage";
import { SLUG_TO_CATEGORY } from "@/lib/itAutomation";

export default function ItAutomationCategoryRoutePage() {
  const params = useParams<{ category: string }>();
  const category = SLUG_TO_CATEGORY[params.category];
  if (!category) {
    return <div className="p-8 text-sm text-foreground/50">Unknown category</div>;
  }
  return <ItAssetCategoryPage category={category} />;
}
