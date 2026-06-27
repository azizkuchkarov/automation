import { DocumentTypeListPage } from "@/components/dcs/DocumentTypeListPage";

export default async function ProcurementTypePage({
  params,
}: {
  params: Promise<{ type: string }>;
}) {
  const { type } = await params;
  return <DocumentTypeListPage typeSlug={type} />;
}
