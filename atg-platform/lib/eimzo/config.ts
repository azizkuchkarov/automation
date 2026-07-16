export const EIMZO_DOMAIN =
  process.env.NEXT_PUBLIC_EIMZO_DOMAIN?.trim() || "unilogin.atg.uz";

export const EIMZO_API_KEY = process.env.NEXT_PUBLIC_EIMZO_API_KEY?.trim() || "";

export function isEimzoConfigured() {
  return getEimzoApiKeyFlat().length >= 2;
}

export function getEimzoApiKeyFlat(): string[] {
  if (!EIMZO_API_KEY) return [];
  return [EIMZO_DOMAIN, EIMZO_API_KEY];
}
