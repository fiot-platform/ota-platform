'use client'

import * as React from 'react'
import Link from 'next/link'
import { ShieldX, ArrowLeft, Home } from 'lucide-react'

export default function UnauthorizedPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-900 via-primary-800 to-danger-900 flex items-center justify-center p-4">
      <div className="bg-white/95 backdrop-blur-sm rounded-2xl shadow-2xl p-10 max-w-md w-full text-center border border-white/20">
        <div className="w-20 h-20 bg-danger-100 rounded-full flex items-center justify-center mx-auto mb-6">
          <ShieldX className="w-10 h-10 text-danger-600" />
        </div>

        <h1 className="text-3xl font-bold text-primary-900 mb-2">Access Denied</h1>
        <p className="text-slate-500 mb-2">
          You do not have permission to view this page.
        </p>
        <p className="text-sm text-slate-400 mb-8">
          Your current role does not grant access to this resource. Contact your administrator if you believe this is an error.
        </p>

        <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
          <Link
            href="/dashboard"
            className="btn-primary w-full sm:w-auto"
          >
            <Home className="w-4 h-4" />
            Back to Dashboard
          </Link>
          <button
            onClick={() => window.history.back()}
            className="btn-secondary w-full sm:w-auto"
          >
            <ArrowLeft className="w-4 h-4" />
            Go Back
          </button>
        </div>

        <div className="mt-8 p-4 bg-slate-50 rounded-lg text-left">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-2">
            What you can do:
          </p>
          <ul className="text-sm text-slate-600 space-y-1">
            <li className="flex items-start gap-2">
              <span className="text-danger-400 mt-0.5">•</span>
              Contact your platform administrator to request access
            </li>
            <li className="flex items-start gap-2">
              <span className="text-danger-400 mt-0.5">•</span>
              Verify you are logged in with the correct account
            </li>
            <li className="flex items-start gap-2">
              <span className="text-danger-400 mt-0.5">•</span>
              Return to an area you have permission to access
            </li>
          </ul>
        </div>
      </div>
    </div>
  )
}
