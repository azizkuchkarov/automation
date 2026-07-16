import { redirect } from "next/navigation";

export default async function HelpdeskQueueRedirect({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  redirect(`/${locale}/helpdesk`);
}
