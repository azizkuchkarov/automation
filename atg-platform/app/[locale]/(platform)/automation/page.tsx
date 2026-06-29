import { redirect } from "next/navigation";

export default async function AutomationIndexPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  redirect(`/${locale}/automation/procurement/requests`);
}
