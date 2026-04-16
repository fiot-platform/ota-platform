'use client'

import { WifiOff, RefreshCw } from 'lucide-react'

export default function OfflinePage() {
  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
      <div className="text-center max-w-md">
        <div className="flex justify-center mb-6">
          <div className="w-20 h-20 rounded-full bg-blue-100 flex items-center justify-center">
            <WifiOff className="w-10 h-10 text-blue-600" />
          </div>
        </div>
        <h1 className="text-2xl font-bold text-slate-900 mb-2">You&apos;re offline</h1>
        <p className="text-slate-500 mb-8">
          No internet connection detected. Please check your network and try again.
        </p>
        <button
          onClick={() => window.location.reload()}
          className="inline-flex items-center gap-2 px-5 py-2.5 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 transition-colors"
        >
          <RefreshCw className="w-4 h-4" />
          Try again
        </button>
      </div>
    </div>
  )
}
