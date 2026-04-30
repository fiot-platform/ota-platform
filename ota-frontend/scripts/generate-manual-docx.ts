#!/usr/bin/env ts-node
/**
 * OTA Platform — Word Document (DOCX) User Manual Generator
 *
 * Generates a properly formatted .docx user manual at:
 *   user-manual/OTA-Platform-User-Manual.docx
 *
 * Embeds screenshots from user-manual/screenshots/ inline.
 * Run AFTER capturing screenshots:
 *   npm run capture-screenshots   (Playwright)
 *   npm run generate-manual-docx  (this script)
 *
 * Or in one shot: npm run manual-docx
 */

import * as fs   from 'fs'
import * as path from 'path'
import {
  Document,
  Packer,
  Paragraph,
  TextRun,
  HeadingLevel,
  ImageRun,
  AlignmentType,
  BorderStyle,
  ShadingType,
  TableRow,
  TableCell,
  Table,
  WidthType,
  VerticalAlign,
  convertInchesToTwip,
  PageNumber,
  Footer,
  Header,
  PageBreak,
} from 'docx'

// ─── Config ───────────────────────────────────────────────────────────────────

const SCREENSHOTS_DIR = path.join(process.cwd(), 'user-manual', 'screenshots')
const OUTPUT_FILE     = path.join(process.cwd(), 'user-manual', 'OTA-Platform-User-Manual.docx')
const GENERATED_DATE  = new Date().toLocaleDateString('en-IN', {
  year: 'numeric', month: 'long', day: 'numeric',
})

// Brand colours (hex without #)
const BRAND_DARK   = '0F172A'  // primary-900
const BRAND_ACCENT = '0EA5E9'  // accent-600
const BRAND_LIGHT  = 'F0F9FF'  // accent-50
const BRAND_TEXT   = '1E293B'  // slate-800
const BRAND_MUTED  = '64748B'  // slate-500
const SUCCESS      = '16A34A'
const WARNING      = 'D97706'
const DANGER       = 'DC2626'
const WHITE        = 'FFFFFF'

// ─── Helpers ──────────────────────────────────────────────────────────────────

function findScreenshot(dir: string, prefix: string): Buffer | null {
  const sectionDir = path.join(SCREENSHOTS_DIR, dir)
  if (!fs.existsSync(sectionDir)) return null
  const files = fs.readdirSync(sectionDir).filter(f => f.startsWith(prefix) && f.endsWith('.png'))
  if (files.length === 0) return null
  return fs.readFileSync(path.join(sectionDir, files[0]))
}

function screenshotParagraph(dir: string, prefix: string, captionText: string): Paragraph[] {
  const buf = findScreenshot(dir, prefix)
  if (!buf) {
    return [
      new Paragraph({
        children: [
          new TextRun({
            text: `📸 Screenshot not yet captured: ${captionText}`,
            italics: true,
            color: BRAND_MUTED,
            size: 18,
          }),
        ],
        spacing: { before: 80, after: 80 },
        shading: { type: ShadingType.SOLID, color: 'FFF9C4', fill: 'FFF9C4' },
      }),
    ]
  }

  return [
    new Paragraph({
      children: [
        new ImageRun({
          data: buf,
          transformation: { width: 600, height: 380 },
          type: 'png',
        }),
      ],
      alignment: AlignmentType.CENTER,
      spacing: { before: 120, after: 40 },
      border: {
        top:    { style: BorderStyle.SINGLE, size: 1, color: 'CBD5E1' },
        bottom: { style: BorderStyle.SINGLE, size: 1, color: 'CBD5E1' },
        left:   { style: BorderStyle.SINGLE, size: 1, color: 'CBD5E1' },
        right:  { style: BorderStyle.SINGLE, size: 1, color: 'CBD5E1' },
      },
    }),
    new Paragraph({
      children: [
        new TextRun({
          text: `Figure: ${captionText}`,
          italics: true,
          color: BRAND_MUTED,
          size: 18,
        }),
      ],
      alignment: AlignmentType.CENTER,
      spacing: { before: 0, after: 200 },
    }),
  ]
}

function heading1(text: string, icon = ''): Paragraph {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    spacing: { before: 400, after: 120 },
    children: [
      new TextRun({
        text: icon ? `${icon}  ${text}` : text,
        bold: true,
        size: 36,
        color: BRAND_DARK,
      }),
    ],
    border: {
      bottom: { style: BorderStyle.SINGLE, size: 4, color: BRAND_ACCENT },
    },
  })
}

function heading2(text: string): Paragraph {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    spacing: { before: 280, after: 80 },
    children: [
      new TextRun({ text, bold: true, size: 28, color: BRAND_DARK }),
    ],
  })
}

function heading3(text: string): Paragraph {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    spacing: { before: 200, after: 60 },
    children: [
      new TextRun({ text, bold: true, size: 24, color: BRAND_TEXT }),
    ],
  })
}

function body(text: string, colour = BRAND_TEXT): Paragraph {
  return new Paragraph({
    children: [new TextRun({ text, size: 22, color: colour })],
    spacing: { before: 40, after: 80 },
  })
}

function infoBox(text: string): Paragraph {
  return new Paragraph({
    children: [new TextRun({ text, size: 21, color: BRAND_DARK, italics: true })],
    spacing: { before: 80, after: 80 },
    indent: { left: convertInchesToTwip(0.3) },
    shading: { type: ShadingType.SOLID, color: BRAND_LIGHT, fill: BRAND_LIGHT },
    border: {
      left: { style: BorderStyle.THICK, size: 12, color: BRAND_ACCENT },
    },
  })
}

function stepItem(num: number, text: string): Paragraph {
  return new Paragraph({
    children: [
      new TextRun({ text: `${num}.  `, bold: true, size: 22, color: BRAND_ACCENT }),
      new TextRun({ text, size: 22, color: BRAND_TEXT }),
    ],
    spacing: { before: 40, after: 40 },
    indent: { left: convertInchesToTwip(0.3) },
  })
}

function divider(): Paragraph {
  return new Paragraph({
    spacing: { before: 160, after: 160 },
    border: { bottom: { style: BorderStyle.SINGLE, size: 1, color: 'E2E8F0' } },
    children: [],
  })
}

// ─── Role / Permission Table ──────────────────────────────────────────────────

function buildRoleTable(): Table {
  const roles = ['SuperAdmin', 'PlatformAdmin', 'ReleaseManager', 'QA', 'CustomerAdmin', 'Viewer']
  const modules = [
    ['Dashboard',      '✅ Full', '✅ Full',   '👁 View', '👁 View', '👁 View', '👁 View'],
    ['Projects',       '✅ Full', '✅ No Del', '👁 View', '👁 View', '👁 View', '👁 View'],
    ['Repositories',   '✅ Full', '✅ No Del', '👁 View', '👁 View', '❌ None', '👁 View'],
    ['Firmware',       '✅ Full', '✅ Full',   '✅ Appv', '✅ QA',   '👁 View', '👁 View'],
    ['Devices',        '✅ Full', '✅ Full',   '👁 View', '❌ None', '👁 View', '👁 View'],
    ['OTA Rollouts',   '✅ Full', '✅ Full',   '✅ Exec', '👁 View', '👁 View', '👁 View'],
    ['Users',          '✅ Full', '✅ No Del', '❌ None', '❌ None', '❌ None', '❌ None'],
    ['Audit Logs',     '✅ Full', '📤 Export','👁 View', '👁 View', '👁 View', '❌ None'],
    ['Reports',        '✅ Full', '📤 Export','👁 View', '👁 View', '👁 View', '👁 View'],
    ['Webhook Events', '✅ Full', '✅ Reproc', '❌ None', '❌ None', '❌ None', '❌ None'],
    ['System Settings','✅ Full', '👁 View',  '❌ None', '❌ None', '❌ None', '❌ None'],
  ]

  const colWidths = [1800, 1200, 1200, 1400, 1200, 1300, 1200]

  const headerRow = new TableRow({
    tableHeader: true,
    children: ['Module', ...roles].map((txt, i) =>
      new TableCell({
        width: { size: colWidths[i], type: WidthType.DXA },
        shading: { type: ShadingType.SOLID, color: BRAND_DARK, fill: BRAND_DARK },
        verticalAlign: VerticalAlign.CENTER,
        children: [
          new Paragraph({
            alignment: AlignmentType.CENTER,
            children: [new TextRun({ text: txt, bold: true, size: 18, color: WHITE })],
          }),
        ],
      })
    ),
  })

  const dataRows = modules.map((row, rowIdx) =>
    new TableRow({
      children: row.map((cell, colIdx) =>
        new TableCell({
          width: { size: colWidths[colIdx], type: WidthType.DXA },
          shading: rowIdx % 2 === 0
            ? { type: ShadingType.SOLID, color: WHITE, fill: WHITE }
            : { type: ShadingType.SOLID, color: 'F8FAFC', fill: 'F8FAFC' },
          verticalAlign: VerticalAlign.CENTER,
          children: [
            new Paragraph({
              alignment: colIdx === 0 ? AlignmentType.LEFT : AlignmentType.CENTER,
              children: [
                new TextRun({
                  text: cell,
                  size: 18,
                  bold: colIdx === 0,
                  color: colIdx === 0 ? BRAND_TEXT : BRAND_MUTED,
                }),
              ],
            }),
          ],
        })
      ),
    })
  )

  return new Table({
    width: { size: 100, type: WidthType.PERCENTAGE },
    rows: [headerRow, ...dataRows],
  })
}

// ─── Section Content ──────────────────────────────────────────────────────────

interface SubSection {
  title: string
  description: string
  screenshotDir: string
  screenshotPrefix: string
  steps?: string[]
}

function buildSubSection(sub: SubSection): Paragraph[] {
  const paragraphs: Paragraph[] = [
    heading3(sub.title),
    infoBox(sub.description),
  ]

  if (sub.steps && sub.steps.length > 0) {
    sub.steps.forEach((step, i) => {
      paragraphs.push(stepItem(i + 1, step))
    })
  }

  paragraphs.push(...screenshotParagraph(sub.screenshotDir, sub.screenshotPrefix, sub.title))
  return paragraphs
}

// ─── Manual Content Definition ────────────────────────────────────────────────

const SECTIONS: {
  icon: string; title: string; intro: string; subsections: SubSection[]
}[] = [
  {
    icon: '🚀', title: '1. Getting Started',
    intro: 'The OTA Platform is a web-based firmware update management system. Access it from any modern browser at your organisation\'s configured URL.',
    subsections: [
      {
        title: 'Login', description: 'Sign in with your email address and password.',
        screenshotDir: '01-authentication', screenshotPrefix: '01-login-page',
        steps: [
          'Open the OTA Platform URL in your browser.',
          'Enter your registered Email Address.',
          'Enter your Password.',
          'Click Sign In.',
          'You will be redirected to the Dashboard upon successful authentication.',
        ],
      },
      {
        title: 'Form Validation', description: 'The login form validates input before submission.',
        screenshotDir: '01-authentication', screenshotPrefix: '02-login-validation-errors',
        steps: [
          'If the email format is invalid, an inline error is shown.',
          'If password is empty, a "required" error is shown.',
          'Wrong credentials show a descriptive error message.',
        ],
      },
    ],
  },
  {
    icon: '📊', title: '2. Dashboard',
    intro: 'The Dashboard provides a real-time overview of the entire OTA platform — active devices, pending firmware approvals, rollout progress, and recent activity.',
    subsections: [
      {
        title: 'Dashboard Overview', description: 'KPI cards and charts at a glance.',
        screenshotDir: '02-dashboard', screenshotPrefix: '01-dashboard-overview',
        steps: [
          'View total Devices, Active Rollouts, Pending Approvals, and Firmware Versions.',
          'Charts show firmware approval trends and rollout success rates.',
          'Recent activity log shows the latest platform actions.',
        ],
      },
      {
        title: 'Sidebar Navigation', description: 'Collapsible sidebar gives access to all modules.',
        screenshotDir: '02-dashboard', screenshotPrefix: '02-sidebar-expanded',
        steps: [
          'Click the ‹ toggle to collapse the sidebar to icon-only mode.',
          'Click › to expand it again.',
          'Your user name and role badge appear at the bottom.',
          'Click Sign Out to end your session.',
        ],
      },
    ],
  },
  {
    icon: '📁', title: '3. Projects',
    intro: 'Projects organise firmware and devices by customer or business unit. Each project has its own repositories, devices, and firmware lifecycle.',
    subsections: [
      {
        title: 'Projects List', description: 'View all projects you have access to.',
        screenshotDir: '03-projects', screenshotPrefix: '01-projects-list',
        steps: [
          'Navigate to Management → Projects.',
          'Use the search bar to filter by name.',
          'Click a project row to view its details.',
          'Status indicators show Active / Inactive state.',
        ],
      },
      {
        title: 'Create Project', description: 'SuperAdmin and PlatformAdmin can create projects.',
        screenshotDir: '03-projects', screenshotPrefix: '02-create-project-dialog',
        steps: [
          'Click Create Project in the top-right corner.',
          'Fill in Project Name and optional Description.',
          'Click Save.',
        ],
      },
      {
        title: 'Project Detail', description: 'View linked repositories, firmware, and devices.',
        screenshotDir: '03-projects', screenshotPrefix: '03-project-detail',
        steps: [
          'Click a project name to open its detail page.',
          'The detail page shows linked repositories and firmware counts.',
          'Use Edit to update metadata. Use Deactivate to soft-delete.',
        ],
      },
    ],
  },
  {
    icon: '🗄️', title: '4. Repositories',
    intro: 'Repositories connect the OTA Platform to your Gitea instances. Webhooks automatically import firmware versions on new releases.',
    subsections: [
      {
        title: 'Repositories List', description: 'All connected Gitea repositories and sync status.',
        screenshotDir: '04-repositories', screenshotPrefix: '01-repositories-list',
        steps: [
          'Navigate to Management → Repositories.',
          'Each row shows the repository name, linked project, and last sync time.',
        ],
      },
      {
        title: 'Add Repository', description: 'Link a Gitea repository to the platform.',
        screenshotDir: '04-repositories', screenshotPrefix: '02-add-repository-dialog',
        steps: [
          'Click Add Repository.',
          'Enter Gitea Owner and Repository Name.',
          'Select the linked Project.',
          'The platform registers a webhook in Gitea automatically.',
          'Click Save — incoming releases will now create firmware versions.',
        ],
      },
      {
        title: 'Repository Detail & Sync', description: 'View firmware and trigger manual sync.',
        screenshotDir: '04-repositories', screenshotPrefix: '03-repository-detail',
        steps: [
          'Click a repository name to open its detail page.',
          'The Firmware Versions tab lists firmware from Gitea releases.',
          'Click Sync Now to pull the latest releases manually.',
        ],
      },
    ],
  },
  {
    icon: '💾', title: '5. Firmware Management',
    intro: 'Firmware versions go through a strict lifecycle: Draft → Pending QA → QA Verified → Pending Approval → Approved / Rejected.',
    subsections: [
      {
        title: 'Firmware List', description: 'Browse all firmware versions with status filtering.',
        screenshotDir: '05-firmware', screenshotPrefix: '01-firmware-list',
        steps: [
          'Navigate to Management → Firmware.',
          'Use the status dropdown to filter: All, Draft, Pending QA, Approved, etc.',
          'Click a firmware version to open its detail page.',
        ],
      },
      {
        title: 'Firmware Detail', description: 'Version metadata, changelog, and approval workflow.',
        screenshotDir: '05-firmware', screenshotPrefix: '02-firmware-detail',
        steps: [
          'Version, Release Date, SHA-256 checksum, and download link are shown.',
          'The Approval Status badge shows the current lifecycle state.',
          'Action buttons change based on your role and the current status.',
        ],
      },
      {
        title: 'Approve Firmware', description: 'ReleaseManager and above can approve firmware.',
        screenshotDir: '05-firmware', screenshotPrefix: '03-approve-firmware-dialog',
        steps: [
          'Open a firmware version in Pending Approval or QA Verified state.',
          'Click Approve.',
          'Optionally add approval notes.',
          'Click Confirm Approval — firmware becomes Approved and ready for rollout.',
          'To reject, click Reject and provide a mandatory rejection reason.',
        ],
      },
      {
        title: 'QA Verification Panel', description: 'QA team verifies firmware before approval.',
        screenshotDir: '05-firmware', screenshotPrefix: '04-qa-verify-panel',
        steps: [
          'Open a firmware in Pending QA state.',
          'Click the QA tab or Start QA Session.',
          'Log test events and upload evidence documents.',
          'Add bugs with severity: Critical, High, Medium, Low.',
          'Click Complete QA to transition firmware to QA Verified.',
        ],
      },
    ],
  },
  {
    icon: '📱', title: '6. Device Management',
    intro: 'Devices are IoT endpoints that receive OTA firmware updates. They are registered, assigned to projects, and monitored for update status.',
    subsections: [
      {
        title: 'Devices List', description: 'View all registered devices with firmware and status.',
        screenshotDir: '06-devices', screenshotPrefix: '01-devices-list',
        steps: [
          'Navigate to Management → Devices.',
          'Search by Device ID, name, or serial number.',
          'Filter by status: Active, Suspended, Decommissioned.',
          'Current Firmware column shows the installed version.',
        ],
      },
      {
        title: 'Register Device', description: 'Add a device individually or in bulk.',
        screenshotDir: '06-devices', screenshotPrefix: '02-register-device-dialog',
        steps: [
          'Click Register Device.',
          'Enter a unique Device ID (hardware identifier).',
          'Enter Device Name and assign to a Project.',
          'Click Save — the device receives a JWT credential.',
          'For bulk registration, click Bulk Import and upload a CSV.',
        ],
      },
      {
        title: 'Device Detail', description: 'Inspect full metadata, firmware, and OTA history.',
        screenshotDir: '06-devices', screenshotPrefix: '03-device-detail',
        steps: [
          'Click a device ID to open its detail page.',
          'Overview tab shows: Device ID, Status, Current Firmware, Last Seen.',
          'Use Edit to update metadata.',
          'Use Suspend or Decommission from the Actions menu.',
        ],
      },
      {
        title: 'OTA History', description: 'Full history of firmware update jobs on this device.',
        screenshotDir: '06-devices', screenshotPrefix: '04-device-ota-history',
        steps: [
          'Click the OTA History tab on a device detail page.',
          'Each row shows: firmware version, rollout name, status, and timestamp.',
          'Statuses: Pending, InProgress, Completed, Failed, Skipped.',
        ],
      },
    ],
  },
  {
    icon: '🔄', title: '7. OTA Rollouts',
    intro: 'A Rollout is a firmware deployment campaign targeting a group of devices, with configurable batch sizes, retries, and scheduling.',
    subsections: [
      {
        title: 'Rollouts List', description: 'View all rollout campaigns and execution status.',
        screenshotDir: '07-ota-rollouts', screenshotPrefix: '01-rollouts-list',
        steps: [
          'Navigate to Operations → OTA Rollouts.',
          'Status badges show: Draft, InProgress, Paused, Completed, Cancelled.',
          'Progress bar shows the percentage of device jobs completed.',
        ],
      },
      {
        title: 'Create Rollout', description: 'Define a new firmware deployment campaign.',
        screenshotDir: '07-ota-rollouts', screenshotPrefix: '02-create-rollout-dialog',
        steps: [
          'Click Create Rollout.',
          'Select the Firmware Version to deploy (must be Approved).',
          'Choose the Target: All devices, by Group, by Site, or specific Device IDs.',
          'Select a Rollout Policy (batch size, retry count, timeout).',
          'Optionally schedule a Start Time — leave blank to start immediately.',
          'Click Save to create the rollout in Draft state.',
        ],
      },
      {
        title: 'Rollout Detail & Control', description: 'Monitor progress and manage execution.',
        screenshotDir: '07-ota-rollouts', screenshotPrefix: '03-rollout-detail',
        steps: [
          'Click a rollout name to open its detail page.',
          'Summary shows total devices, completed, failed, and in-progress counts.',
          'Click Start to begin execution (requires ReleaseManager role or above).',
          'Click Pause to halt execution temporarily.',
          'Click Resume to continue a paused rollout.',
          'Click Cancel to abort — this cannot be undone.',
        ],
      },
      {
        title: 'Device Job List', description: 'Per-device job status table.',
        screenshotDir: '07-ota-rollouts', screenshotPrefix: '04-rollout-job-list',
        steps: [
          'Scroll down on the rollout detail page to see the Jobs table.',
          'Each row represents a single device OTA job.',
          'Failed jobs can be retried individually.',
          'Click a Device ID to navigate to that device\'s detail page.',
        ],
      },
    ],
  },
  {
    icon: '👥', title: '8. User Management',
    intro: 'Administrators can manage platform users, assign roles, and control project-scope access.',
    subsections: [
      {
        title: 'Users List', description: 'View all users and their assigned roles.',
        screenshotDir: '08-users', screenshotPrefix: '01-users-list',
        steps: [
          'Navigate to Administration → Users.',
          'Search by name or email.',
          'Filter by Role using the dropdown.',
          'Status badges show Active or Deactivated.',
        ],
      },
      {
        title: 'Create User', description: 'Invite a new user and assign their role.',
        screenshotDir: '08-users', screenshotPrefix: '02-create-user-dialog',
        steps: [
          'Click Invite User / Create User.',
          'Enter Full Name and Email Address.',
          'Select the Role: SuperAdmin, PlatformAdmin, ReleaseManager, QA, CustomerAdmin, or Viewer.',
          'Set a temporary password.',
          'Click Save.',
        ],
      },
      {
        title: 'User Detail', description: 'View and edit user profile, role, and project scope.',
        screenshotDir: '08-users', screenshotPrefix: '03-user-detail',
        steps: [
          'Click a user to open their detail page.',
          'Use Edit to change the full name.',
          'Use Assign Role to change the platform role.',
          'Use Deactivate to disable login without deleting the account.',
        ],
      },
    ],
  },
  {
    icon: '📋', title: '9. Audit Logs',
    intro: 'Every create, update, delete, approve, and execute action is recorded in an immutable audit log for compliance.',
    subsections: [
      {
        title: 'Audit Logs', description: 'Browse and filter the full audit trail.',
        screenshotDir: '09-audit-logs', screenshotPrefix: '01-audit-logs-list',
        steps: [
          'Navigate to Administration → Audit Logs.',
          'Filter by Date Range, User, Action Type, or Entity Type.',
          'Each entry shows: Timestamp, User, Action, Entity, and Detail.',
          'Click Export to download logs as CSV or JSON.',
        ],
      },
    ],
  },
  {
    icon: '📈', title: '10. Reports',
    intro: 'The Reports section provides analytical insights across firmware, devices, rollouts, and users.',
    subsections: [
      {
        title: 'Firmware Approval Trends', description: 'Approval trend chart over time.',
        screenshotDir: '10-reports', screenshotPrefix: '01-firmware-trends',
        steps: [
          'Navigate to Reports → Firmware Trends.',
          'Select a date range.',
          'Chart shows daily/weekly approval and rejection counts.',
        ],
      },
      {
        title: 'Rollout Success Rate', description: 'Success vs failure metrics.',
        screenshotDir: '10-reports', screenshotPrefix: '02-rollout-success',
        steps: [
          'Navigate to Reports → Rollout Success.',
          'Success rate = (Completed Jobs / Total Jobs) × 100.',
          'Export data as CSV.',
        ],
      },
      {
        title: 'Device Status Report', description: 'Pie chart of device states.',
        screenshotDir: '10-reports', screenshotPrefix: '03-device-status',
        steps: [
          'Navigate to Reports → Device Status.',
          'Donut chart shows Active, Suspended, Decommissioned proportions.',
          'Filter by Project to scope the report.',
        ],
      },
      {
        title: 'Daily OTA Progress', description: 'Day-by-day OTA job counts.',
        screenshotDir: '10-reports', screenshotPrefix: '04-daily-ota-progress',
        steps: [
          'Navigate to Reports → Daily OTA Progress.',
          'Select a date range.',
          'Chart shows daily totals — useful for identifying deployment spikes.',
        ],
      },
      {
        title: 'Firmware Stage Distribution', description: 'Firmware versions per lifecycle stage.',
        screenshotDir: '10-reports', screenshotPrefix: '05-firmware-stage-distribution',
        steps: [
          'Navigate to Reports → Firmware Stage.',
          'Chart shows how many versions are in: Draft, Pending QA, Approved, Deprecated.',
        ],
      },
    ],
  },
  {
    icon: '🔗', title: '11. Webhook Events',
    intro: 'Gitea webhook events are received and processed automatically. Developers can inspect payloads and reprocess failed events.',
    subsections: [
      {
        title: 'Webhook Events', description: 'View received Gitea events and their status.',
        screenshotDir: '11-webhook-events', screenshotPrefix: '01-webhook-events-list',
        steps: [
          'Navigate to Developer → Webhook Events.',
          'Filter by Status: Received, Processing, Processed, Failed.',
          'Click an event to view the raw JSON payload.',
          'For Failed events, click Reprocess to retry handling.',
        ],
      },
    ],
  },
  {
    icon: '⚙️', title: '12. Settings & Profile',
    intro: 'Personal and platform-level configuration settings.',
    subsections: [
      {
        title: 'Email Notifications', description: 'Control which emails you receive.',
        screenshotDir: '12-settings', screenshotPrefix: '01-email-notifications-settings',
        steps: [
          'Navigate to Administration → Settings → Email Notifications.',
          'Toggle notifications for: Firmware Approved, Rollout Completed, Device Failures, etc.',
          'Click Save to apply preferences.',
        ],
      },
      {
        title: 'Profile', description: 'View your profile and change your password.',
        screenshotDir: '12-settings', screenshotPrefix: '02-profile-page',
        steps: [
          'Click your name in the sidebar footer, or navigate to /profile.',
          'Your role, email, and linked Gitea profile are displayed.',
          'Use the Change Password section to update your password.',
        ],
      },
    ],
  },
]

// ─── Build Document ───────────────────────────────────────────────────────────

async function buildDocument(): Promise<Document> {
  const children: (Paragraph | Table)[] = []

  // ── Cover Page ──────────────────────────────────────────────────────────────
  children.push(
    new Paragraph({
      spacing: { before: 1200, after: 200 },
      shading: { type: ShadingType.SOLID, color: BRAND_DARK, fill: BRAND_DARK },
      children: [new TextRun({ text: 'OTA PLATFORM', bold: true, size: 72, color: WHITE })],
      alignment: AlignmentType.CENTER,
    }),
    new Paragraph({
      spacing: { before: 80, after: 80 },
      children: [new TextRun({ text: 'User Manual', bold: true, size: 48, color: BRAND_ACCENT })],
      alignment: AlignmentType.CENTER,
    }),
    new Paragraph({
      spacing: { before: 80, after: 80 },
      children: [new TextRun({ text: 'Enterprise Over-The-Air Firmware Update Management', size: 26, color: BRAND_MUTED, italics: true })],
      alignment: AlignmentType.CENTER,
    }),
    new Paragraph({
      spacing: { before: 160, after: 80 },
      children: [new TextRun({ text: `Generated: ${GENERATED_DATE}`, size: 22, color: BRAND_MUTED })],
      alignment: AlignmentType.CENTER,
    }),
    new Paragraph({
      spacing: { before: 40, after: 1200 },
      children: [new TextRun({ text: 'Version 1.0  |  Confidential', size: 22, color: BRAND_MUTED })],
      alignment: AlignmentType.CENTER,
    }),
    new Paragraph({ children: [new PageBreak()] })
  )

  // ── Firmware Lifecycle Callout ──────────────────────────────────────────────
  children.push(
    heading1('Firmware Lifecycle', '💾'),
    infoBox(
      'Draft  →  Pending QA  →  QA Verified  →  Pending Approval  →  Approved / Rejected  →  Deprecated'
    ),
    divider()
  )

  // ── Role & Permission Overview ──────────────────────────────────────────────
  children.push(
    heading1('Role & Permission Overview', '🔐'),
    body('The OTA Platform uses Role-Based Access Control (RBAC). Your role determines which modules and actions you can access.'),
    new Paragraph({ children: [], spacing: { before: 120, after: 0 } }),
    buildRoleTable(),
    divider(),
  )

  // ── Sections ────────────────────────────────────────────────────────────────
  for (const section of SECTIONS) {
    children.push(
      new Paragraph({ children: [new PageBreak()] }),
      heading1(section.title, section.icon),
      infoBox(section.intro),
    )
    for (const sub of section.subsections) {
      children.push(...buildSubSection(sub))
    }
    children.push(divider())
  }

  return new Document({
    title: 'OTA Platform User Manual',
    description: 'Enterprise Firmware Update Management — User Manual',
    creator: 'OTA Platform',
    sections: [
      {
        properties: {
          page: {
            margin: {
              top:    convertInchesToTwip(1),
              right:  convertInchesToTwip(1),
              bottom: convertInchesToTwip(1),
              left:   convertInchesToTwip(1.25),
            },
          },
        },
        headers: {
          default: new Header({
            children: [
              new Paragraph({
                children: [
                  new TextRun({ text: 'OTA Platform — User Manual', size: 18, color: BRAND_MUTED }),
                  new TextRun({ text: '    |    ', size: 18, color: 'CBD5E1' }),
                  new TextRun({ text: 'Confidential', size: 18, color: BRAND_MUTED, italics: true }),
                ],
                border: { bottom: { style: BorderStyle.SINGLE, size: 1, color: 'E2E8F0' } },
              }),
            ],
          }),
        },
        footers: {
          default: new Footer({
            children: [
              new Paragraph({
                alignment: AlignmentType.CENTER,
                border: { top: { style: BorderStyle.SINGLE, size: 1, color: 'E2E8F0' } },
                children: [
                  new TextRun({ text: 'Page ', size: 18, color: BRAND_MUTED }),
                  new TextRun({
                    children: [PageNumber.CURRENT],
                    size: 18,
                    color: BRAND_MUTED,
                  }),
                  new TextRun({ text: ' of ', size: 18, color: BRAND_MUTED }),
                  new TextRun({
                    children: [PageNumber.TOTAL_PAGES],
                    size: 18,
                    color: BRAND_MUTED,
                  }),
                ],
              }),
            ],
          }),
        },
        // Role table is inserted as a table, not mixed with paragraph children
        children: [
          ...children.slice(0, children.length),
        ],
      },
    ],
  })
}

// ─── Main ─────────────────────────────────────────────────────────────────────

async function main() {
  console.log('📝 OTA Platform — Word Manual Generator')
  console.log(`📁 Screenshots dir : ${SCREENSHOTS_DIR}`)
  console.log(`📄 Output file     : ${OUTPUT_FILE}`)

  let screenshotCount = 0
  if (fs.existsSync(SCREENSHOTS_DIR)) {
    const walk = (dir: string) => {
      fs.readdirSync(dir, { withFileTypes: true }).forEach(e => {
        if (e.isDirectory()) walk(path.join(dir, e.name))
        else if (e.name.endsWith('.png')) screenshotCount++
      })
    }
    walk(SCREENSHOTS_DIR)
  }
  console.log(`📸 Found ${screenshotCount} screenshots\n`)

  const outputDir = path.dirname(OUTPUT_FILE)
  if (!fs.existsSync(outputDir)) fs.mkdirSync(outputDir, { recursive: true })

  const doc    = await buildDocument()
  const buffer = await Packer.toBuffer(doc)
  fs.writeFileSync(OUTPUT_FILE, buffer)

  const sizeKb = Math.round(fs.statSync(OUTPUT_FILE).size / 1024)
  console.log(`✅ Word manual generated: ${OUTPUT_FILE}`)
  console.log(`   Size: ${sizeKb} KB`)
  console.log('\n💡 Open in Microsoft Word or LibreOffice.')
  console.log('   To regenerate: npm run generate-manual-docx')
}

main().catch(err => {
  console.error('❌ Generator failed:', err)
  process.exit(1)
})
