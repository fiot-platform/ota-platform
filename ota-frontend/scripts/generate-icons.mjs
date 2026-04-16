/**
 * Icon generation script for PWA icons.
 * Run: node scripts/generate-icons.mjs
 *
 * Requires: npm install -D sharp
 */

import sharp from 'sharp'
import { readFileSync, mkdirSync } from 'fs'
import { join, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const rootDir = join(__dirname, '..')
const iconsDir = join(rootDir, 'public', 'icons')

mkdirSync(iconsDir, { recursive: true })

const svgBuffer = readFileSync(join(iconsDir, 'icon.svg'))

const sizes = [72, 96, 128, 144, 152, 192, 384, 512]

async function generateIcons() {
  for (const size of sizes) {
    await sharp(svgBuffer)
      .resize(size, size)
      .png()
      .toFile(join(iconsDir, `icon-${size}x${size}.png`))
    console.log(`Generated icon-${size}x${size}.png`)
  }

  // Maskable icons (with padding ~10% safe zone)
  for (const size of [192, 512]) {
    const padding = Math.floor(size * 0.1)
    const innerSize = size - padding * 2
    await sharp(svgBuffer)
      .resize(innerSize, innerSize)
      .extend({
        top: padding,
        bottom: padding,
        left: padding,
        right: padding,
        background: { r: 22, g: 101, b: 52, alpha: 1 }, // #166534
      })
      .png()
      .toFile(join(iconsDir, `icon-maskable-${size}x${size}.png`))
    console.log(`Generated icon-maskable-${size}x${size}.png`)
  }

  // Apple touch icon
  await sharp(svgBuffer)
    .resize(180, 180)
    .png()
    .toFile(join(iconsDir, 'apple-touch-icon.png'))
  console.log('Generated apple-touch-icon.png')

  // Favicon
  await sharp(svgBuffer)
    .resize(32, 32)
    .png()
    .toFile(join(rootDir, 'public', 'favicon-32x32.png'))
  console.log('Generated favicon-32x32.png')

  await sharp(svgBuffer)
    .resize(16, 16)
    .png()
    .toFile(join(rootDir, 'public', 'favicon-16x16.png'))
  console.log('Generated favicon-16x16.png')

  console.log('\nAll icons generated successfully!')
}

generateIcons().catch(console.error)
