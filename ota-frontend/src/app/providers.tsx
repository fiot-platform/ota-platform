'use client'

import { useState } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { ToastProvider } from '@/components/ui/ToastProvider'
import { NotificationProvider } from '@/context/NotificationContext'

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30 * 1000,       // 30 s — queries refetch at most every 30 s
            gcTime: 5 * 60 * 1000,      // 5 min — keep unused data in memory
            refetchOnWindowFocus: false, // avoid spurious refetch on tab switch
            refetchOnReconnect: true,
            retry: (failureCount, error: unknown) => {
              const axiosError = error as { response?: { status?: number } }
              if (axiosError?.response?.status === 401) return false
              if (axiosError?.response?.status === 403) return false
              if (axiosError?.response?.status === 404) return false
              return failureCount < 2
            },
          },
          mutations: {
            retry: false,
          },
        },
      })
  )

  return (
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <NotificationProvider>
          {children}
        </NotificationProvider>
      </ToastProvider>
      {process.env.NODE_ENV === 'development' && (
        <ReactQueryDevtools initialIsOpen={false} />
      )}
    </QueryClientProvider>
  )
}
