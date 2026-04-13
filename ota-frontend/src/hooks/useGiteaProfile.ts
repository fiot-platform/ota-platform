'use client'

import { useQuery } from '@tanstack/react-query'
import { giteaService, GiteaProfile } from '@/services/gitea.service'
import { isAuthenticated } from '@/lib/auth'

export function useGiteaProfile() {
  return useQuery<GiteaProfile | null>({
    queryKey: ['gitea-profile'],
    queryFn: () => giteaService.getMyProfile(),
    enabled: isAuthenticated(),
    staleTime: 5 * 60 * 1000,   // treat as fresh for 5 min
    retry: false,                 // don't retry — Gitea may simply be offline
  })
}
