/**
 * Screenshot Helper — utilities for capturing full-page screenshots
 * used by the user manual generator.
 */
import { Page } from '@playwright/test'
import * as path from 'path'
import * as fs from 'fs'

const SCREENSHOTS_DIR = path.join(process.cwd(), 'user-manual', 'screenshots')

export interface ScreenshotOptions {
  /** Subdirectory within screenshots dir (e.g. 'dashboard') */
  section: string
  /** File name without extension (e.g. 'overview') */
  name: string
  /** Extra wait after navigation before snapshot (ms) */
  waitMs?: number
  /** Hide dynamic/random elements before screenshot */
  maskDynamic?: boolean
  /** Full page or viewport only */
  fullPage?: boolean
}

export async function captureScreenshot(
  page: Page,
  opts: ScreenshotOptions
): Promise<string> {
  const dir = path.join(SCREENSHOTS_DIR, opts.section)
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true })

  if (opts.waitMs) {
    await page.waitForTimeout(opts.waitMs)
  }

  // Mask timestamps, IDs, and "time ago" text to keep screenshots stable
  if (opts.maskDynamic !== false) {
    await page.evaluate(() => {
      document
        .querySelectorAll('[data-testid="timestamp"], time, .relative-time')
        .forEach((el) => ((el as HTMLElement).style.opacity = '0'))
    })
  }

  const filePath = path.join(dir, `${opts.name}.png`)
  await page.screenshot({
    path: filePath,
    fullPage: opts.fullPage ?? true,
    animations: 'disabled',
  })

  return filePath
}

/**
 * Ensure that a page is loaded before taking a screenshot:
 * waits for loading spinners / skeleton loaders to disappear.
 */
export async function waitForPageReady(page: Page): Promise<void> {
  // 'load' fires reliably; networkidle can hang if the app polls the API continuously
  await page.waitForLoadState('load')
  // Give React/TanStack Query time to render initial data
  await page.waitForTimeout(1500)
  // Wait for any Tailwind/radix loading spinner to vanish
  const spinner = page.locator('.animate-spin').first()
  if (await spinner.isVisible({ timeout: 3_000 }).catch(() => false)) {
    await spinner.waitFor({ state: 'hidden', timeout: 15_000 })
  }
}
