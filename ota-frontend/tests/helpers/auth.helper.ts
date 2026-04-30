/**
 * Auth Helper — reusable login/logout utilities for tests that need
 * to operate under a specific role rather than the shared SuperAdmin state.
 */
import { Page, expect } from '@playwright/test'

export interface TestCredentials {
  email: string
  password: string
}

export const TEST_USERS: Record<string, TestCredentials> = {
  superAdmin: {
    email:    process.env.SUPERADMIN_EMAIL    ?? 'admin@otaplatform.com',
    password: process.env.SUPERADMIN_PASSWORD ?? 'Admin@123',
  },
  platformAdmin: {
    email:    process.env.PLATFORM_ADMIN_EMAIL    ?? 'platformadmin@otaplatform.com',
    password: process.env.PLATFORM_ADMIN_PASSWORD ?? 'Admin@123',
  },
  releaseManager: {
    email:    process.env.RELEASE_MANAGER_EMAIL    ?? 'release@otaplatform.com',
    password: process.env.RELEASE_MANAGER_PASSWORD ?? 'Admin@123',
  },
  qa: {
    email:    process.env.QA_EMAIL    ?? 'qa@otaplatform.com',
    password: process.env.QA_PASSWORD ?? 'Admin@123',
  },
  viewer: {
    email:    process.env.VIEWER_EMAIL    ?? 'viewer@otaplatform.com',
    password: process.env.VIEWER_PASSWORD ?? 'Admin@123',
  },
}

/**
 * Login with the given credentials and wait for the dashboard.
 */
export async function login(page: Page, credentials: TestCredentials): Promise<void> {
  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'OTA Platform' })).toBeVisible()

  await page.fill('#email', credentials.email)
  await page.fill('#password', credentials.password)
  await page.click('button[type="submit"]')

  await page.waitForURL('**/dashboard', { timeout: 30_000 })
  await expect(page.getByText('Dashboard', { exact: true }).first()).toBeVisible()
}

/**
 * Logout via the sidebar Sign Out button.
 */
export async function logout(page: Page): Promise<void> {
  // Try the "Sign Out" text link in expanded sidebar
  const signOutBtn = page.getByRole('button', { name: /sign out/i })
  if (await signOutBtn.isVisible()) {
    await signOutBtn.click()
  } else {
    // Collapsed sidebar — find the LogOut icon button
    await page.locator('[title="Sign Out"]').click()
  }
  await page.waitForURL('**/login', { timeout: 15_000 })
}

/**
 * Navigate to a dashboard route and wait for the page to be ready.
 */
export async function navigateTo(page: Page, path: string): Promise<void> {
  await page.goto(path)
  // Wait for the main content area to render (not just navigation)
  await page.waitForLoadState('networkidle')
}
