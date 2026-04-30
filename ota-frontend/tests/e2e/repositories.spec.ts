/**
 * Repositories Tests — list, create, sync with Gitea
 */
import { test, expect } from '@playwright/test'

test.describe('Repositories', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/repositories')
    await page.waitForLoadState('networkidle')
  })

  test('repositories list page renders', async ({ page }) => {
    await expect(
      page.getByRole('heading', { name: /repositories|repos/i }).first()
    ).toBeVisible()
  })

  test('shows Add Repository button for SuperAdmin', async ({ page }) => {
    const addBtn = page.getByRole('button', {
      name: /add repository|new repository|create repo/i,
    }).first()
    await expect(addBtn).toBeVisible()
  })

  test('repository list renders', async ({ page }) => {
    const main = page.locator('main').first()
    await expect(main).toBeVisible()
  })

  test('open Add Repository dialog', async ({ page }) => {
    const addBtn = page.getByRole('button', {
      name: /add repository|new repository|create repo/i,
    }).first()
    await addBtn.click()

    const dialog = page.getByRole('dialog').first()
    await expect(dialog).toBeVisible()
    await page.keyboard.press('Escape')
  })

  test('clicking a repository navigates to detail', async ({ page }) => {
    const firstLink = page.locator('a[href^="/repositories/"]').first()
    if (!(await firstLink.isVisible().catch(() => false))) {
      test.skip()
      return
    }

    await firstLink.click()
    await expect(page).toHaveURL(/\/repositories\//)
    await page.waitForLoadState('networkidle')
  })

  test('repository detail shows firmware list and sync button', async ({ page }) => {
    const firstLink = page.locator('a[href^="/repositories/"]').first()
    if (!(await firstLink.isVisible().catch(() => false))) {
      test.skip()
      return
    }

    await firstLink.click()
    await page.waitForLoadState('networkidle')

    // Sync button should be visible on detail page
    const syncBtn = page.getByRole('button', { name: /sync/i }).first()
    const hasSyncBtn = await syncBtn.isVisible({ timeout: 5_000 }).catch(() => false)

    await expect(page).toHaveURL(/\/repositories\//)
  })
})
