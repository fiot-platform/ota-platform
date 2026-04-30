/**
 * Auth Tests — Login, validation, logout
 * These tests run WITHOUT a pre-loaded auth state.
 */
import { test, expect } from '@playwright/test'

// Override storageState for this file — tests manage their own auth
test.use({ storageState: { cookies: [], origins: [] } })

test.describe('Authentication', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login')
  })

  test('login page renders correctly', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'OTA Platform' })).toBeVisible()
    await expect(page.getByText('Sign in to your admin portal')).toBeVisible()
    await expect(page.locator('#email')).toBeVisible()
    await expect(page.locator('#password')).toBeVisible()
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible()
  })

  test('shows validation errors for empty form submission', async ({ page }) => {
    await page.click('button[type="submit"]')
    await expect(page.getByText(/valid email/i)).toBeVisible()
    await expect(page.getByText(/password is required/i)).toBeVisible()
  })

  test('shows error for invalid email format', async ({ page }) => {
    // Disable browser-native HTML5 validation so React-Hook-Form / Zod can fire
    await page.evaluate(() => {
      const form = document.querySelector('form')
      if (form) form.noValidate = true
    })
    await page.fill('#email', 'not-an-email')
    await page.fill('#password', 'somepassword')
    await page.click('button[type="submit"]')
    await expect(page.getByText(/valid email/i)).toBeVisible()
  })

  test('shows error for wrong credentials', async ({ page }) => {
    await page.fill('#email', 'wrong@example.com')
    await page.fill('#password', 'wrongpassword')

    // Set up the response listener BEFORE clicking so we don't miss it
    const responsePromise = page.waitForResponse(
      (res) => res.url().includes('/auth/login'),
      { timeout: 15_000 },
    )
    await page.click('button[type="submit"]')
    await responsePromise

    // API returns 401/404 — the UI shows inline .form-error below email or password,
    // OR a toast with "Sign in failed". Wait for either.
    await expect(
      page.locator('.form-error, [role="alert"]').first()
    ).toBeVisible({ timeout: 8_000 })
  })

  test('password visibility toggle works', async ({ page }) => {
    await page.fill('#password', 'MySecret123')
    const passwordInput = page.locator('#password')
    await expect(passwordInput).toHaveAttribute('type', 'password')

    // Click the eye icon
    await page.click('button[aria-label="Show password"]')
    await expect(passwordInput).toHaveAttribute('type', 'text')

    // Click again to hide
    await page.click('button[aria-label="Hide password"]')
    await expect(passwordInput).toHaveAttribute('type', 'password')
  })

  test('successful login redirects to dashboard', async ({ page }) => {
    const email    = process.env.SUPERADMIN_EMAIL    ?? 'admin@otaplatform.com'
    const password = process.env.SUPERADMIN_PASSWORD ?? 'Admin@123'

    await page.fill('#email', email)
    await page.fill('#password', password)
    await page.click('button[type="submit"]')

    await page.waitForURL('**/dashboard', { timeout: 30_000 })
    await expect(page).toHaveURL(/dashboard/)
  })

  test('authenticated user is redirected away from login', async ({ page }) => {
    // First login
    const email    = process.env.SUPERADMIN_EMAIL    ?? 'admin@otaplatform.com'
    const password = process.env.SUPERADMIN_PASSWORD ?? 'Admin@123'

    await page.fill('#email', email)
    await page.fill('#password', password)
    await page.click('button[type="submit"]')
    await page.waitForURL('**/dashboard', { timeout: 30_000 })

    // Try to navigate back to login
    await page.goto('/login')
    await expect(page).toHaveURL(/dashboard/)
  })

  test('logout clears session and redirects to login', async ({ page }) => {
    const email    = process.env.SUPERADMIN_EMAIL    ?? 'admin@otaplatform.com'
    const password = process.env.SUPERADMIN_PASSWORD ?? 'Admin@123'

    await page.fill('#email', email)
    await page.fill('#password', password)
    await page.click('button[type="submit"]')
    await page.waitForURL('**/dashboard', { timeout: 30_000 })

    // Sign out
    const signOut = page.getByRole('button', { name: /sign out/i })
    if (await signOut.isVisible()) {
      await signOut.click()
    } else {
      await page.locator('[title="Sign Out"]').click()
    }

    await page.waitForURL('**/login', { timeout: 15_000 })
    await expect(page).toHaveURL(/login/)
  })
})
