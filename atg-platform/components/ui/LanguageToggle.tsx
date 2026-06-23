"use client";

import { useLocale } from "next-intl";
import { usePathname, useRouter } from "next/navigation";

export function LanguageToggle() {
  const locale = useLocale();
  const router = useRouter();
  const pathname = usePathname();

  const switchLocale = (newLocale: string) => {
    const segments = pathname.split("/");
    segments[1] = newLocale;
    router.push(segments.join("/"));
  };

  return (
    <div className="flex rounded-md border border-border text-xs overflow-hidden">
      {["ru", "en"].map((l) => (
        <button
          key={l}
          onClick={() => switchLocale(l)}
          className={`px-2.5 py-1 uppercase ${locale === l ? "bg-atg-blue text-white" : "hover:bg-border/30"}`}
        >
          {l}
        </button>
      ))}
    </div>
  );
}
