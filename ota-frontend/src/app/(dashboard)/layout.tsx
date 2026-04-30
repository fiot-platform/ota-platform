'use client'

import * as React from 'react'
import { useRouter } from 'next/navigation'
import { clsx } from 'clsx'
import { Sidebar } from '@/components/layout/Sidebar'
import { Header } from '@/components/layout/Header'
import { isAuthenticated } from '@/lib/auth'

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const router = useRouter()
  const [collapsed, setCollapsed] = React.useState(false)
  const [mounted, setMounted] = React.useState(false)

  React.useEffect(() => {
    setMounted(true)
    if (!isAuthenticated()) {
      router.push('/login')
    }
  }, [router])

  if (!mounted) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <div className="w-10 h-10 border-4 border-accent-600 border-t-transparent rounded-full animate-spin" />
          <p className="text-sm text-slate-500">Loading...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <Sidebar collapsed={collapsed} onToggle={() => setCollapsed(!collapsed)} />
      <Header sidebarCollapsed={collapsed} />
      {/* Mobile: always offset by w-16. Desktop: offset by collapsed/expanded width */}
      <main
        className={clsx(
          'min-h-screen pt-16 transition-all duration-300',
          'pl-16',
          collapsed ? 'md:pl-16' : 'md:pl-[260px]'
        )}
      >
        <div className="p-4 md:p-6 max-w-[1400px] mx-auto">
          {children}
        </div>
      </main>
    </div>
  )
}
