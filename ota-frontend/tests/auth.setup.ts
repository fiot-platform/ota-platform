/**
 * Auth Setup — runs before all E2E / manual-screenshot tests.
 * Logs in as SuperAdmin and saves the storage state so tests skip
 * the login page entirely.
 */
import { test as setup, expect } from '@playwright/test'
import * as path from 'path'
import * as fs from 'fs'

const AUTH_DIR = path.join(__dirname, '.auth')

setup.beforeAll(() => {
  if (!fs.existsSync(AUTH_DIR)) fs.mkdirSync(AUTH_DIR, { recursive: true })
})

setup('authenticate as SuperAdmin', async ({ page }) => {
  const email    = process.env.SUPERADMIN_EMAIL    ?? 'admin@otaplatform.com'
  const password = process.env.SUPERADMIN_PASSWORD ?? 'Admin@123'

  const PLACEHOLDER = 'FILL_IN_YOUR_PASSWORD_HERE'
  if (password === PLACEHOLDER) {
    console.warn(`
⚠️  SUPERADMIN_PASSWORD is not set in .env.test
   Open ota-frontend/.env.test and set:
     SUPERADMIN_EMAIL=<your-admin-email>
     SUPERADMIN_PASSWORD=<your-admin-password>
   Then re-run: npm test
   Saving EMPTY auth state — all protected-page tests will be skipped.
`)
    // Write an empty auth state so downstream tests can still start
    // (they'll fail/skip when they detect they're not logged in)
    fs.writeFileSync(
      path.join(AUTH_DIR, 'superadmin.json'),
      JSON.stringify({ cookies: [], origins: [] })
    )
    return
  }

  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'OTA Platform' })).toBeVisible()

  await page.fill('#email', email)
  await page.fill('#password', password)
  await page.click('button[type="submit"]')

  // Wait for redirect to dashboard
  await page.waitForURL('**/dashboard', { timeout: 30_000 })
  await expect(page.getByText('Dashboard', { exact: true }).first()).toBeVisible()

  // Save auth state
  await page.context().storageState({
    path: path.join(AUTH_DIR, 'superadmin.json'),
  })

  console.log('✅ SuperAdmin auth state saved')
})
