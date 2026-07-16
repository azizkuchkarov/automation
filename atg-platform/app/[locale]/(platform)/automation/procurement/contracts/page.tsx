import { redirect } from "next/navigation";

export default async function ContractsIndexPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  redirect(`/${locale}/automation/procurement/contracts/local`);
}
