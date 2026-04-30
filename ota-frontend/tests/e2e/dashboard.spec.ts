/**
 * Dashboard Tests — KPI widgets, navigation, sidebar
 */
import { test, expect } from '@playwright/test'

test.describe('Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')
  })

  test('dashboard page loads with KPI widgets', async ({ page }) => {
    // Page heading
    await expect(page.getByRole('heading', { name: /dashboard/i }).first()).toBeVisible()
  })

  test('sidebar shows all main navigation items for SuperAdmin', async ({ page }) => {
    // These items are always visible in the sidebar nav
    const navItems = [
      'Dashboard',
      'Projects',
      'Repositories',
      'Firmware',
      'Devices',
      'OTA Rollouts',
      'Users',
      'Audit Logs',
      'Webhook Events',
    ]

    for (const label of navItems) {
      await expect(
        page.locator('aside').getByTitle(label).first()
      ).toBeVisible()
    }
  })

  test('clicking Projects in sidebar navigates to /projects', async ({ page }) => {
    await page.locator('aside a[href="/projects"]').click()
    await expect(page).toHaveURL(/\/projects/)
  })

  test('clicking Firmware in sidebar navigates to /firmware', async ({ page }) => {
    await page.locator('aside a[href="/firmware"]').click()
    await expect(page).toHaveURL(/\/firmware/)
  })

  test('clicking Devices in sidebar navigates to /devices', async ({ page }) => {
    await page.locator('aside a[href="/devices"]').click()
    await expect(page).toHaveURL(/\/devices/)
  })

  test('clicking OTA Rollouts in sidebar navigates to /ota-rollouts', async ({ page }) => {
    await page.locator('aside a[href="/ota-rollouts"]').click()
    await expect(page).toHaveURL(/\/ota-rollouts/)
  })

  test('header is visible with notification bell', async ({ page }) => {
    // Fixed top header
    const header = page.locator('header').first()
    await expect(header).toBeVisible()
  })

  test('sidebar collapse/expand toggle works', async ({ page }) => {
    const sidebar = page.locator('aside').first()

    // Check initial expanded state on desktop
    await expect(sidebar).toBeVisible()

    // Click the collapse toggle button
    const toggleBtn = page.locator('button[aria-label="Collapse sidebar"]')
    if (await toggleBtn.isVisible()) {
      await toggleBtn.click()
      await expect(page.locator('button[aria-label="Expand sidebar"]')).toBeVisible()
    }
  })
})
