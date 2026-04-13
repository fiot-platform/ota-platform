import { NextRequest, NextResponse } from 'next/server'

// ─── Public routes (no auth required) ────────────────────────────────────────

const PUBLIC_ROUTES = ['/login', '/unauthorized', '/forgot-password', '/reset-password']

// ─── Route-role requirements ──────────────────────────────────────────────────

const ROUTE_ROLE_MAP: Record<string, string[]> = {
  '/users': ['SuperAdmin', 'PlatformAdmin'],
  '/audit-logs': ['SuperAdmin', 'PlatformAdmin', 'ReleaseManager', 'DevOpsEngineer', 'Auditor'],
  '/webhook-events': ['SuperAdmin', 'PlatformAdmin', 'DevOpsEngineer'],
  '/reports': ['SuperAdmin', 'PlatformAdmin', 'ReleaseManager', 'DevOpsEngineer', 'SupportEngineer', 'CustomerAdmin', 'Viewer', 'Auditor'],
}

function parseJwtPayload(token: string): { role?: string; exp?: number } | null {
  try {
    const base64Url = token.split('.')[1]
    if (!base64Url) return null
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const json = Buffer.from(base64, 'base64').toString('utf-8')
    return JSON.parse(json)
  } catch {
    return null
  }
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl

  // Allow public routes and static assets
  if (
    PUBLIC_ROUTES.some((r) => pathname.startsWith(r)) ||
    pathname.startsWith('/_next') ||
    pathname.startsWith('/api') ||
    pathname.startsWith('/favicon') ||
    pathname === '/'
  ) {
    // Redirect root to login or dashboard
    if (pathname === '/') {
      return NextResponse.redirect(new URL('/login', request.url))
    }
    return NextResponse.next()
  }

  // Read JWT from cookie
  const token = request.cookies.get('ota_token')?.value

  if (!token) {
    const loginUrl = new URL('/login', request.url)
    loginUrl.searchParams.set('from', pathname)
    return NextResponse.redirect(loginUrl)
  }

  // Parse token
  const payload = parseJwtPayload(token)

  if (!payload) {
    const loginUrl = new URL('/login', request.url)
    return NextResponse.redirect(loginUrl)
  }

  // Check token expiry
  if (payload.exp && payload.exp * 1000 < Date.now()) {
    const loginUrl = new URL('/login', request.url)
    loginUrl.searchParams.set('reason', 'session_expired')
    const response = NextResponse.redirect(loginUrl)
    response.cookies.delete('ota_token')
    return response
  }

  const userRole = payload.role

  // Check route-specific role requirements
  for (const [route, allowedRoles] of Object.entries(ROUTE_ROLE_MAP)) {
    if (pathname.startsWith(route)) {
      if (!userRole || !allowedRoles.includes(userRole)) {
        return NextResponse.redirect(new URL('/unauthorized', request.url))
      }
      break
    }
  }

  // Device role has no UI access
  if (userRole === 'Device') {
    return NextResponse.redirect(new URL('/unauthorized', request.url))
  }

  // Forward user info as request headers for server components
  const requestHeaders = new Headers(request.headers)
  requestHeaders.set('x-user-role', userRole ?? '')
  requestHeaders.set('x-user-id', payload ? (payload as { userId?: string }).userId ?? '' : '')

  return NextResponse.next({
    request: { headers: requestHeaders },
  })
}

export const config = {
  matcher: [
    '/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)',
  ],
}
