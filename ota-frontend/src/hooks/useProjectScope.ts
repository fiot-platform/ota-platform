'use client'

import { useQuery } from '@tanstack/react-query'
import { authService } from '@/services/auth.service'
import { projectService } from '@/services/project.service'
import { useAuth } from '@/hooks/useAuth'
import { UserRole } from '@/types'

/**
 * Returns the live project scope for the current user, fetched fresh from the
 * database (not the JWT) so admin changes take effect immediately without re-login.
 *
 * For QA role: always returns the first assigned project ID (or null if none),
 * plus the project name for display purposes.
 * For all other roles: returns null (no restriction applied).
 */
export function useProjectScope(): {
  scopedProjectId: string | null
  scopedProjectName: string | null
  isScoped: boolean
  isLoading: boolean
} {
  const { role } = useAuth()
  const isQA = role === UserRole.QA

  const { data: liveUser, isLoading: userLoading } = useQuery({
    queryKey: ['auth-me'],
    queryFn: () => authService.getCurrentUser(),
    enabled: isQA,
    staleTime: 30_000,
  })

  const scopedProjectId = isQA ? (liveUser?.projectScope?.[0] ?? null) : null

  const { data: project, isLoading: projectLoading } = useQuery({
    queryKey: ['project', scopedProjectId],
    queryFn: () => projectService.getProjectById(scopedProjectId!),
    enabled: isQA && !!scopedProjectId,
    staleTime: 60_000,
  })

  if (!isQA) {
    return { scopedProjectId: null, scopedProjectName: null, isScoped: false, isLoading: false }
  }

  return {
    scopedProjectId,
    scopedProjectName: project?.name ?? null,
    isScoped: true,
    isLoading: userLoading || projectLoading,
  }
}
