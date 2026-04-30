/**
 * User Manual — Screenshot Capture Spec
 *
 * Run with:
 *   npx playwright test --project=manual-screenshots
 *
 * Captures full-page screenshots of every major page and saves them to
 * user-manual/screenshots/<section>/<name>.png
 *
 * The generate-manual script then assembles these into an HTML user manual.
 */
import { test, expect } from '@playwright/test'
import { captureScreenshot, waitForPageReady } from '../helpers/screenshot.helper'

// ─── Helper ──────────────────────────────────────────────────────────────────

async function screenshot(
  page: Parameters<typeof captureScreenshot>[0],
  section: string,
  name: string,
  waitMs = 1500
) {
  await waitForPageReady(page)
  await captureScreenshot(page, { section, name, waitMs, fullPage: true })
  console.log(`  📸 ${section}/${name}.png`)
}

// ─── 1. Authentication ────────────────────────────────────────────────────────

test.describe('01 - Authentication', () => {
  // Run these without stored auth so we see the actual login page
  test.use({ storageState: { cookies: [], origins: [] } })

  test('01-login-page', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('load')
    await screenshot(page, '01-authentication', '01-login-page')
  })

  test('02-login-validation-errors', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('load')
    await page.click('button[type="submit"]')
    await page.waitForTimeout(500)
    await screenshot(page, '01-authentication', '02-login-validation-errors')
  })
})

// ─── 2. Dashboard ─────────────────────────────────────────────────────────────

test.describe('02 - Dashboard', () => {
  test('01-dashboard-overview', async ({ page }) => {
    await page.goto('/dashboard')
    await screenshot(page, '02-dashboard', '01-dashboard-overview')
  })

  test('02-sidebar-expanded', async ({ page }) => {
    await page.goto('/dashboard')
    await waitForPageReady(page)
    // Ensure sidebar is expanded
    const expandBtn = page.locator('button[aria-label="Expand sidebar"]')
    if (await expandBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await expandBtn.click()
      await page.waitForTimeout(400)
    }
    await screenshot(page, '02-dashboard', '02-sidebar-expanded')
  })

  test('03-sidebar-collapsed', async ({ page }) => {
    await page.goto('/dashboard')
    await waitForPageReady(page)
    const collapseBtn = page.locator('button[aria-label="Collapse sidebar"]')
    if (await collapseBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await collapseBtn.click()
      await page.waitForTimeout(400)
    }
    await screenshot(page, '02-dashboard', '03-sidebar-collapsed')
  })
})

// ─── 3. Projects ──────────────────────────────────────────────────────────────

test.describe('03 - Projects', () => {
  test('01-projects-list', async ({ page }) => {
    await page.goto('/projects')
    await screenshot(page, '03-projects', '01-projects-list')
  })

  test('02-create-project-dialog', async ({ page }) => {
    await page.goto('/projects')
    await waitForPageReady(page)
    const createBtn = page.getByRole('button', {
      name: /create project|new project|add project/i,
    }).first()
    if (await createBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await createBtn.click()
      await page.waitForTimeout(400)
      await screenshot(page, '03-projects', '02-create-project-dialog')
      await page.keyboard.press('Escape')
    }
  })

  test('03-project-detail', async ({ page }) => {
    await page.goto('/projects')
    await waitForPageReady(page)
    const firstLink = page.locator('a[href^="/projects/"]').first()
    if (await firstLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstLink.click()
      await screenshot(page, '03-projects', '03-project-detail')
    }
  })
})

// ─── 4. Repositories ─────────────────────────────────────────────────────────

test.describe('04 - Repositories', () => {
  test('01-repositories-list', async ({ page }) => {
    await page.goto('/repositories')
    await screenshot(page, '04-repositories', '01-repositories-list')
  })

  test('02-add-repository-dialog', async ({ page }) => {
    await page.goto('/repositories')
    await waitForPageReady(page)
    const addBtn = page.getByRole('button', {
      name: /add repository|new repository|create repo/i,
    }).first()
    if (await addBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await addBtn.click()
      await page.waitForTimeout(400)
      await screenshot(page, '04-repositories', '02-add-repository-dialog')
      await page.keyboard.press('Escape')
    }
  })

  test('03-repository-detail', async ({ page }) => {
    await page.goto('/repositories')
    await waitForPageReady(page)
    const firstLink = page.locator('a[href^="/repositories/"]').first()
    if (await firstLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstLink.click()
      await screenshot(page, '04-repositories', '03-repository-detail')
    }
  })
})

// ─── 5. Firmware ─────────────────────────────────────────────────────────────

test.describe('05 - Firmware', () => {
  test('01-firmware-list', async ({ page }) => {
    await page.goto('/firmware')
    await screenshot(page, '05-firmware', '01-firmware-list')
  })

  test('02-firmware-detail', async ({ page }) => {
    await page.goto('/firmware')
    await waitForPageReady(page)
    const firstLink = page.locator('a[href^="/firmware/"]').first()
    if (await firstLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstLink.click()
      await screenshot(page, '05-firmware', '02-firmware-detail')
    }
  })

  test('03-approve-firmware-dialog', async ({ page }) => {
    await page.goto('/firmware')
    await waitForPageReady(page)
    const firstLink = page.locator('a[href^="/firmware/"]').first()
    if (await firstLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstLink.click()
      await waitForPageReady(page)
      const approveBtn = page.getByRole('button', { name: /approve/i }).first()
      if (await approveBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await approveBtn.click()
        await page.waitForTimeout(400)
        await screenshot(page, '05-firmware', '03-approve-firmware-dialog')
        await page.keyboard.press('Escape')
      }
    }
  })

  test('04-qa-verify-panel', async ({ page }) => {
    await page.goto('/firmware')
    await waitForPageReady(page)
    const firstLink = page.locator('a[href^="/firmware/"]').first()
    if (await firstLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstLink.click()
      await waitForPageReady(page)
      const qaTab = page.getByRole('tab', { name: /qa|verification/i }).first()
      if (await qaTab.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await qaTab.click()
        await page.waitForTimeout(500)
        await screenshot(page, '05-firmware', '04-qa-verify-panel')
      }
    }
  })
})

// ─── 6. Devices ──────────────────────────────────────────────────────────────

test.describe('06 - Devices', () => {
  test('01-devices-list', async ({ page }) => {
    await page.goto('/devices')
    await screenshot(page, '06-devices', '01-devices-list')
  })

  test('02-register-device-dialog', async ({ page }) => {
    await page.goto('/devices')
    await waitForPageReady(page)
    const registerBtn = page.getByRole('button', {
      name: /register|add device|new device/i,
    }).first()
    if (await registerBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await registerBtn.click()
      await page.waitForTimeout(400)
      await screenshot(page, '06-devices', '02-register-device-dialog')
      await page.keyboard.press('Escape')
    }
  })

  test('03-device-detail', async ({ page }) => {
    await page.goto('/devices')
    await waitForPageReady(page)
    const firstLink = page.locator('a[href^="/devices/"]').first()
    if (await firstLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstLink.click()
      await screenshot(page, '06-devices', '03-device-detail')
    }
  })

  test('04-device-ota-history', async ({ page }) => {
    await page.goto('/devices')
    await waitForPageReady(page)
    const firstLink = page.locator('a[href^="/devices/"]').first()
    if (await firstLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstLink.click()
      await waitForPageReady(page)
      const otaTab = page.getByRole('tab', { name: /ota history|history/i }).first()
      if (await otaTab.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await otaTab.click()
        await page.waitForTimeout(400)
        await screenshot(page, '06-devices', '04-device-ota-history')
      }
    }
  })
})

// ─── 7. OTA Rollouts ─────────────────────────────────────────────────────────

test.describe('07 - OTA Rollouts', () => {
  test('01-rollouts-list', async ({ page }) => {
    await page.goto('/ota-rollouts')
    await screenshot(page, '07-ota-rollouts', '01-rollouts-list')
  })

  test('02-create-rollout-dialog', async ({ page }) => {
    await page.goto('/ota-rollouts')
    await waitForPageReady(page)
    const createBtn = page.getByRole('button', {
      name: /create rollout|new rollout/i,
    }).first()
    if (await createBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await createBtn.click()
      await page.waitForTimeout(400)
      await screenshot(page, '07-ota-rollouts', '02-create-rollout-dialog')
      await page.keyboard.press('Escape')
    }
  })

  test('03-rollout-detail', async ({ page }) => {
    await page.goto('/ota-rollouts')
    await waitForPageReady(page)
    const firstLink = page.locator('a[href^="/ota-rollouts/"]').first()
    if (await firstLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstLink.click()
      await screenshot(page, '07-ota-rollouts', '03-rollout-detail')
    }
  })

  test('04-rollout-job-list', async ({ page }) => {
    await page.goto('/ota-rollouts')
    await waitForPageReady(page)
    const firstLink = page.locator('a[href^="/ota-rollouts/"]').first()
    if (await firstLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstLink.click()
      await waitForPageReady(page)
      // Scroll to jobs section if present
      const jobsSection = page.getByText(/jobs|devices/i).first()
      if (await jobsSection.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await jobsSection.scrollIntoViewIfNeeded()
        await page.waitForTimeout(300)
        await screenshot(page, '07-ota-rollouts', '04-rollout-job-list')
      }
    }
  })
})

// ─── 8. User Management ──────────────────────────────────────────────────────

test.describe('08 - User Management', () => {
  test('01-users-list', async ({ page }) => {
    await page.goto('/users')
    await screenshot(page, '08-users', '01-users-list')
  })

  test('02-create-user-dialog', async ({ page }) => {
    await page.goto('/users')
    await waitForPageReady(page)
    const createBtn = page.getByRole('button', {
      name: /invite user|create user|add user/i,
    }).first()
    if (await createBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await createBtn.click()
      await page.waitForTimeout(400)
      await screenshot(page, '08-users', '02-create-user-dialog')
      await page.keyboard.press('Escape')
    }
  })

  test('03-user-detail', async ({ page }) => {
    await page.goto('/users')
    await waitForPageReady(page)
    const firstLink = page.locator('a[href^="/users/"]').first()
    if (await firstLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstLink.click()
      await screenshot(page, '08-users', '03-user-detail')
    }
  })
})

// ─── 9. Audit Logs ───────────────────────────────────────────────────────────

test.describe('09 - Audit Logs', () => {
  test('01-audit-logs-list', async ({ page }) => {
    await page.goto('/audit-logs')
    await screenshot(page, '09-audit-logs', '01-audit-logs-list')
  })
})

// ─── 10. Reports ─────────────────────────────────────────────────────────────

test.describe('10 - Reports', () => {
  test('01-firmware-trends', async ({ page }) => {
    await page.goto('/reports/firmware-trends')
    await screenshot(page, '10-reports', '01-firmware-trends', 2000)
  })

  test('02-rollout-success', async ({ page }) => {
    await page.goto('/reports/rollout-success')
    await screenshot(page, '10-reports', '02-rollout-success', 2000)
  })

  test('03-device-status', async ({ page }) => {
    await page.goto('/reports/device-status')
    await screenshot(page, '10-reports', '03-device-status', 2000)
  })

  test('04-daily-ota-progress', async ({ page }) => {
    await page.goto('/reports/daily-progress')
    await screenshot(page, '10-reports', '04-daily-ota-progress', 2000)
  })

  test('05-firmware-stage-distribution', async ({ page }) => {
    await page.goto('/reports/firmware-stage')
    await screenshot(page, '10-reports', '05-firmware-stage-distribution', 2000)
  })

  test('06-device-ota-history', async ({ page }) => {
    await page.goto('/reports/device-ota')
    await screenshot(page, '10-reports', '06-device-ota-history', 2000)
  })
})

// ─── 11. Webhook Events ──────────────────────────────────────────────────────

test.describe('11 - Webhook Events', () => {
  test('01-webhook-events-list', async ({ page }) => {
    await page.goto('/webhook-events')
    await screenshot(page, '11-webhook-events', '01-webhook-events-list')
  })
})

// ─── 12. Settings & Profile ──────────────────────────────────────────────────

test.describe('12 - Settings & Profile', () => {
  test('01-email-notifications-settings', async ({ page }) => {
    await page.goto('/settings/email-notifications')
    await screenshot(page, '12-settings', '01-email-notifications-settings')
  })

  test('02-profile-page', async ({ page }) => {
    await page.goto('/profile')
    await screenshot(page, '12-settings', '02-profile-page')
  })
})
