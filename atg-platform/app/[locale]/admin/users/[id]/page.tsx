import UserFormPage from "../new/page";

export default function EditUserPage({ params }: { params: Promise<{ id: string }> }) {
  return <UserFormPage params={params} />;
}
