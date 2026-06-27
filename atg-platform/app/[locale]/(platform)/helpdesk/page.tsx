import { redirect } from "next/navigation";

export default async function HelpdeskIndex({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  redirect(`/${locale}/helpdesk/board`);
}
