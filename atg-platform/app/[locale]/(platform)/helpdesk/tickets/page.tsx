import { redirect } from "next/navigation";

export default async function HelpdeskTicketsRedirect({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  redirect(`/${locale}/helpdesk`);
}
