import api from '@/lib/api'
import { ApiResponse } from '@/types'

export interface GiteaProfile {
  id: number
  login: string
  full_name: string
  email: string
  avatar_url: string
}

export const giteaService = {
  async getMyProfile(): Promise<GiteaProfile | null> {
    const response = await api.get<ApiResponse<GiteaProfile | null>>('/auth/gitea-profile')
    return response.data.data ?? null
  },
}
