import api from '@/lib/api'
import { ApiResponse } from '@/types'

// ─── Types ────────────────────────────────────────────────────────────────────

export interface EmailNotificationSettings {
  onFirmwareSubmitted: boolean
  onFirmwareApproved: boolean
  onFirmwareRejected: boolean
  onFirmwareQAVerified: boolean
  onRolloutStarted: boolean
  onRolloutCompleted: boolean
  onRolloutFailed: boolean
  onDeviceOtaFailed: boolean
  onDeviceRegistered: boolean
  onNewUserCreated: boolean
  onUserDeactivated: boolean
  notifyEmails: string[]
  updatedAt?: string
  updatedBy?: string
}

export type UpdateEmailNotificationSettingsRequest = Omit<
  EmailNotificationSettings,
  'updatedAt' | 'updatedBy'
>

// ─── Service ──────────────────────────────────────────────────────────────────

export const settingsService = {
  /**
   * GET /api/settings/email-notifications
   * Returns current email notification settings (platform defaults if none saved).
   */
  async getEmailNotificationSettings(): Promise<EmailNotificationSettings> {
    const response = await api.get<ApiResponse<EmailNotificationSettings>>(
      '/settings/email-notifications'
    )
    return response.data.data
  },

  /**
   * PUT /api/settings/email-notifications
   * Saves the email notification settings and returns the persisted state.
   */
  async updateEmailNotificationSettings(
    data: UpdateEmailNotificationSettingsRequest
  ): Promise<EmailNotificationSettings> {
    const response = await api.put<ApiResponse<EmailNotificationSettings>>(
      '/settings/email-notifications',
      data
    )
    return response.data.data
  },
}
