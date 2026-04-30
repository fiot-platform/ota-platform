// @ts-check
const withPWAInit = require('@ducanh2912/next-pwa').default

const withPWA = withPWAInit({
  dest: 'public',
  cacheOnFrontEndNav: true,
  aggressiveFrontEndNavCaching: true,
  reloadOnOnline: true,
  disable: process.env.NODE_ENV === 'development',
  dynamicStartUrlRedirect: '/login',
  fallbacks: {
    document: '/offline',
  },
  workboxOptions: {
    disableDevLogs: true,
  },
})

/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  images: {
    domains: [
      'localhost',
      'gitea.example.com',
      'cdn.example.com',
      'avatars.githubusercontent.com',
    ],
    remotePatterns: [
      {
        protocol: 'https',
        hostname: '**.example.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '3021',
        pathname: '/**',
      },
      {
        protocol: 'http',
        hostname: 'iot.ssmsportal.com',
        port: '',
        pathname: '/**',
      },
    ],
  },
  env: {
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api',
  },
  async headers() {
    return [
      {
        source: '/(.*)',
        headers: [
          { key: 'X-Frame-Options', value: 'DENY' },
          { key: 'X-Content-Type-Options', value: 'nosniff' },
          { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
          { key: 'Permissions-Policy', value: 'camera=(), microphone=(), geolocation=()' },
        ],
      },
    ]
  },
  async rewrites() {
    return [
      {
        source: '/api/proxy/:path*',
        destination: 'http://iot.ssmsportal.com:5001/api/:path*',
      },
    ]
  },
  experimental: {
    typedRoutes: false,
  },
}

module.exports = withPWA(nextConfig)
