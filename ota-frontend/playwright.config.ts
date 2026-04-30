import { defineConfig, devices } from '@playwright/test'
import * as path from 'path'

/**
 * OTA Platform — Playwright Configuration
 * Covers: E2E functional tests + user-manual screenshot capture
 */

// Load .env.test if present
require('dotenv').config({ path: path.resolve(__dirname, '.env.test') })

const BASE_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:3000'

export default defineConfig({
  testDir: './tests',
  /* Run tests in sub-directories */
  fullyParallel: false,          // auth state is shared — keep sequential
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : 1,
  timeout: 60_000,
  expect: { timeout: 15_000 },

  reporter: [
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['list'],
  ],

  use: {
    baseURL: BASE_URL,
    /* Capture trace on first retry */
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    /* Desktop viewport for manual screenshots */
    viewport: { width: 1440, height: 900 },
    /* Slow down actions for visual clarity */
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },

  projects: [
    /* ── Auth setup (runs first, stores auth state) ─────────────────────── */
    {
      name: 'auth-setup',
      testMatch: /auth\.setup\.ts/,
    },

    /* ── E2E Functional Tests ────────────────────────────────────────────── */
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'tests/.auth/superadmin.json',
      },
      dependencies: ['auth-setup'],
      testMatch: /e2e\/.*\.spec\.ts/,
    },

    /* ── User Manual Screenshot Capture ─────────────────────────────────── */
    {
      name: 'manual-screenshots',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'tests/.auth/superadmin.json',
        viewport: { width: 1440, height: 900 },
      },
      dependencies: ['auth-setup'],
      testMatch: /manual\/.*\.spec\.ts/,
    },
  ],

  /* Start Next.js dev server automatically when running locally */
  webServer: process.env.CI
    ? undefined
    : {
        command: 'npm run dev',
        url: BASE_URL,
        reuseExistingServer: true,
        timeout: 120_000,
      },
})
