'use client'

import { FolderOpen } from 'lucide-react'
import Link from 'next/link'

interface ProjectScopeBannerProps {
  projectId: string
  projectName: string | null
}

/**
 * Shown at the top of scoped pages (Firmware, Repositories, Devices, OTA Rollouts)
 * when the current user is a QA engineer with a single assigned project.
 * Makes it clear that all data on the page belongs to this project.
 */
export function ProjectScopeBanner({ projectId, projectName }: ProjectScopeBannerProps) {
  return (
    <div className="flex items-center gap-3 px-4 py-3 rounded-xl bg-accent-50 border border-accent-100">
      <div className="w-7 h-7 rounded-lg bg-accent-100 flex items-center justify-center flex-shrink-0">
        <FolderOpen className="w-4 h-4 text-accent-600" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs font-semibold text-accent-700 uppercase tracking-wide">Assigned Project</p>
        <p className="text-sm font-bold text-accent-900 truncate">
          {projectName ?? 'Loading…'}
        </p>
      </div>
      <Link
        href={`/projects/${projectId}`}
        className="text-xs font-medium text-accent-600 hover:text-accent-800 transition-colors whitespace-nowrap"
      >
        View project →
      </Link>
    </div>
  )
}
