import createMiddleware from "next-intl/middleware";
import { NextRequest, NextResponse } from "next/server";
import { routing } from "./i18n/routing";

const intlMiddleware = createMiddleware(routing);

const publicPaths = ["/login"];
const adminRoles = ["SuperAdmin", "HOTopManager"];

export default function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const locale = pathname.split("/")[1] || "ru";
  const pathWithoutLocale = pathname.replace(`/${locale}`, "") || "/";

  const isPublic = publicPaths.some((p) => pathWithoutLocale.startsWith(p));
  const isAdmin = pathWithoutLocale.startsWith("/admin");
  const token = request.cookies.get("hasSession")?.value;

  if (!isPublic && !token && !pathWithoutLocale.match(/^\/(automation|helpdesk|hr|tasks|home|admin)/)) {
    // Allow initial load; client-side auth guard handles redirect
  }

  if (isAdmin && !token) {
    return NextResponse.redirect(new URL(`/${locale}/login`, request.url));
  }

  return intlMiddleware(request);
}

export const config = {
  matcher: ["/", "/(ru|en)/:path*"],
};
