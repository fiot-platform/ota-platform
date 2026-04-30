#!/usr/bin/env ts-node
/**
 * OTA Platform — User Manual Generator
 *
 * Reads screenshots from user-manual/screenshots/ and produces a
 * self-contained HTML user manual at user-manual/OTA-Platform-User-Manual.html
 *
 * Usage (after running screenshot capture):
 *   npx ts-node scripts/generate-manual.ts
 *   OR
 *   npm run generate-manual
 */

import * as fs   from 'fs'
import * as path from 'path'

const SCREENSHOTS_DIR = path.join(process.cwd(), 'user-manual', 'screenshots')
const OUTPUT_FILE     = path.join(process.cwd(), 'user-manual', 'OTA-Platform-User-Manual.html')
const GENERATED_DATE  = new Date().toLocaleDateString('en-IN', {
  year: 'numeric', month: 'long', day: 'numeric',
})

// ─── Section Metadata ─────────────────────────────────────────────────────────

interface ScreenshotMeta {
  file: string     // relative path from screenshots dir
  title: string
  description: string
}

interface ManualSection {
  id: string
  icon: string
  title: string
  intro: string
  subsections: {
    title: string
    description: string
    screenshotDir: string
    screenshotPrefix: string
    steps?: string[]
  }[]
}

const MANUAL_SECTIONS: ManualSection[] = [
  {
    id: 'getting-started',
    icon: '🚀',
    title: 'Getting Started',
    intro: 'The OTA Platform is a web-based firmware update management system. Access it from any modern browser at your organisation\'s configured URL.',
    subsections: [
      {
        title: 'Login',
        description: 'Sign in with your email address and password.',
        screenshotDir: '01-authentication',
        screenshotPrefix: '01-login-page',
        steps: [
          'Open the OTA Platform URL in your browser.',
          'Enter your registered Email Address.',
          'Enter your Password.',
          'Click Sign In.',
          'You will be redirected to the Dashboard upon successful authentication.',
        ],
      },
      {
        title: 'Form Validation',
        description: 'The login form validates your input before submission.',
        screenshotDir: '01-authentication',
        screenshotPrefix: '02-login-validation-errors',
        steps: [
          'If the email format is invalid, an inline error is shown.',
          'If the password field is empty, a required error is shown.',
          'Wrong credentials show a descriptive error message.',
        ],
      },
    ],
  },
  {
    id: 'dashboard',
    icon: '📊',
    title: 'Dashboard',
    intro: 'The Dashboard provides a real-time overview of the entire OTA platform — active devices, pending firmware approvals, rollout progress, and recent activity.',
    subsections: [
      {
        title: 'Dashboard Overview',
        description: 'KPI cards and charts are displayed at a glance for quick platform health monitoring.',
        screenshotDir: '02-dashboard',
        screenshotPrefix: '01-dashboard-overview',
        steps: [
          'View total Devices, Active Rollouts, Pending Approvals, and Firmware Versions at the top.',
          'Charts show firmware approval trends and rollout success rates.',
          'Recent activity log shows the latest platform actions.',
        ],
      },
      {
        title: 'Sidebar Navigation (Expanded)',
        description: 'The sidebar gives access to all modules. It shows section labels and full menu text when expanded.',
        screenshotDir: '02-dashboard',
        screenshotPrefix: '02-sidebar-expanded',
        steps: [
          'Click the ‹ toggle button at the top-right of the sidebar to collapse it.',
          'Click › to expand it again.',
          'Your user name and role badge appear at the bottom.',
          'Click Sign Out to end your session.',
        ],
      },
    ],
  },
  {
    id: 'projects',
    icon: '📁',
    title: 'Projects',
    intro: 'Projects organise your firmware and devices by customer or business unit. Each project has its own repositories, devices, and firmware lifecycle.',
    subsections: [
      {
        title: 'Projects List',
        description: 'View all projects you have access to. Use the search bar to filter by name.',
        screenshotDir: '03-projects',
        screenshotPrefix: '01-projects-list',
        steps: [
          'Navigate to Management → Projects in the sidebar.',
          'Use the search bar to filter projects by name.',
          'Click on a project row to view its details.',
          'Status indicators show Active / Inactive state.',
        ],
      },
      {
        title: 'Create Project',
        description: 'SuperAdmin and PlatformAdmin can create new projects.',
        screenshotDir: '03-projects',
        screenshotPrefix: '02-create-project-dialog',
        steps: [
          'Click Create Project in the top-right corner.',
          'Fill in the Project Name and optional Description.',
          'Select the Customer if multi-tenant.',
          'Click Save to create the project.',
        ],
      },
      {
        title: 'Project Detail',
        description: 'View repositories, firmware versions, and devices linked to a project.',
        screenshotDir: '03-projects',
        screenshotPrefix: '03-project-detail',
        steps: [
          'Click a project name to open its detail page.',
          'The detail page shows linked repositories and firmware counts.',
          'Use the Edit button to update project metadata.',
          'Deactivate to soft-delete without removing historical data.',
        ],
      },
    ],
  },
  {
    id: 'repositories',
    icon: '🗄️',
    title: 'Repositories',
    intro: 'Repositories connect the OTA Platform to your Gitea instances. When a new release or tag is pushed to Gitea, a webhook automatically imports the firmware version.',
    subsections: [
      {
        title: 'Repositories List',
        description: 'All connected Gitea repositories and their sync status.',
        screenshotDir: '04-repositories',
        screenshotPrefix: '01-repositories-list',
        steps: [
          'Navigate to Management → Repositories.',
          'Each row shows the repository name, linked project, and last sync time.',
          'A status badge shows whether the webhook is active.',
        ],
      },
      {
        title: 'Add Repository',
        description: 'Link a Gitea repository to the platform.',
        screenshotDir: '04-repositories',
        screenshotPrefix: '02-add-repository-dialog',
        steps: [
          'Click Add Repository.',
          'Enter the Gitea Owner and Repository Name.',
          'Select the linked Project.',
          'The platform will automatically register a webhook in Gitea.',
          'Click Save — incoming releases will now create firmware versions automatically.',
        ],
      },
      {
        title: 'Repository Detail & Manual Sync',
        description: 'View firmware versions imported from this repository and trigger a manual sync.',
        screenshotDir: '04-repositories',
        screenshotPrefix: '03-repository-detail',
        steps: [
          'Click a repository name to open its detail page.',
          'The Firmware Versions tab lists all firmware pulled from Gitea releases.',
          'Click Sync Now to pull the latest releases manually.',
          'Each synced release appears as a Draft firmware version.',
        ],
      },
    ],
  },
  {
    id: 'firmware',
    icon: '💾',
    title: 'Firmware Management',
    intro: 'Firmware versions go through a strict approval lifecycle before they can be deployed to devices: Draft → Pending QA → QA Verified → Pending Approval → Approved / Rejected.',
    subsections: [
      {
        title: 'Firmware List',
        description: 'Browse all firmware versions across projects with status filtering.',
        screenshotDir: '05-firmware',
        screenshotPrefix: '01-firmware-list',
        steps: [
          'Navigate to Management → Firmware.',
          'Use the status tabs to filter: All, Draft, Pending QA, Approved, etc.',
          'Click on a firmware version to view its details and start the approval workflow.',
        ],
      },
      {
        title: 'Firmware Detail',
        description: 'Version metadata, changelog, and the approval workflow are shown on the detail page.',
        screenshotDir: '05-firmware',
        screenshotPrefix: '02-firmware-detail',
        steps: [
          'Version, Release Date, SHA-256 checksum, and download link are displayed.',
          'The Approval Status badge shows the current workflow state.',
          'Action buttons change based on your role and the current status.',
        ],
      },
      {
        title: 'Approve Firmware',
        description: 'ReleaseManager and above can approve firmware after QA verification.',
        screenshotDir: '05-firmware',
        screenshotPrefix: '03-approve-firmware-dialog',
        steps: [
          'Open a firmware version in Pending Approval state.',
          'Click Approve.',
          'Optionally enter approval notes.',
          'Click Confirm Approval — the firmware becomes Approved and is ready for rollout.',
          'To reject, click Reject and provide a mandatory rejection reason.',
        ],
      },
      {
        title: 'QA Verification Panel',
        description: 'The QA team verifies firmware by running tests, logging bugs, and uploading evidence documents.',
        screenshotDir: '05-firmware',
        screenshotPrefix: '04-qa-verify-panel',
        steps: [
          'Open a firmware version in Pending QA state.',
          'Click the QA tab or Start QA Session.',
          'Log test events using the event log.',
          'Add bugs with severity: Critical, High, Medium, Low.',
          'Upload test reports or evidence documents.',
          'Click Complete QA to transition the firmware to QA Verified.',
        ],
      },
    ],
  },
  {
    id: 'devices',
    icon: '📱',
    title: 'Device Management',
    intro: 'Devices are the IoT endpoints that receive firmware updates. They are registered in the platform, assigned to projects, and monitored for OTA update status.',
    subsections: [
      {
        title: 'Devices List',
        description: 'View all registered devices with their current firmware version and status.',
        screenshotDir: '06-devices',
        screenshotPrefix: '01-devices-list',
        steps: [
          'Navigate to Management → Devices.',
          'Search by Device ID, name, or serial number.',
          'Filter by status: Active, Suspended, Decommissioned.',
          'The Current Firmware column shows the installed version.',
          'Last Seen shows when the device last contacted the platform.',
        ],
      },
      {
        title: 'Register Device',
        description: 'Add a new device to the platform individually or in bulk.',
        screenshotDir: '06-devices',
        screenshotPrefix: '02-register-device-dialog',
        steps: [
          'Click Register Device.',
          'Enter a unique Device ID (hardware identifier).',
          'Enter the Device Name and assign it to a Project.',
          'Set the Initial Firmware Version if known.',
          'Click Save — the device will receive a JWT credential for API communication.',
          'For bulk registration, click the Bulk Import button and upload a CSV.',
        ],
      },
      {
        title: 'Device Detail',
        description: 'Inspect a device\'s full metadata, current firmware, and OTA job history.',
        screenshotDir: '06-devices',
        screenshotPrefix: '03-device-detail',
        steps: [
          'Click on a device ID to open its detail page.',
          'The Overview tab shows: Device ID, Status, Current Firmware, Last Seen.',
          'Use the Edit button to update device name or metadata.',
          'Use the Suspend or Decommission actions from the Actions menu.',
        ],
      },
      {
        title: 'OTA History',
        description: 'Full history of firmware update jobs that ran on this device.',
        screenshotDir: '06-devices',
        screenshotPrefix: '04-device-ota-history',
        steps: [
          'Click the OTA History tab on a device detail page.',
          'Each row shows: firmware version, rollout name, status, and timestamp.',
          'Statuses include: Pending, InProgress, Completed, Failed, Skipped.',
        ],
      },
    ],
  },
  {
    id: 'ota-rollouts',
    icon: '🔄',
    title: 'OTA Rollouts',
    intro: 'A Rollout is a firmware deployment campaign that targets a group of devices. It orchestrates the distribution process with configurable batch sizes, retries, and scheduling.',
    subsections: [
      {
        title: 'Rollouts List',
        description: 'View all rollout campaigns with their current execution status.',
        screenshotDir: '07-ota-rollouts',
        screenshotPrefix: '01-rollouts-list',
        steps: [
          'Navigate to Operations → OTA Rollouts.',
          'Status badges show: Draft, InProgress, Paused, Completed, Cancelled.',
          'Progress bar shows the percentage of device jobs completed.',
          'Click on a rollout to view its detail and job status.',
        ],
      },
      {
        title: 'Create Rollout',
        description: 'Define a new firmware deployment campaign.',
        screenshotDir: '07-ota-rollouts',
        screenshotPrefix: '02-create-rollout-dialog',
        steps: [
          'Click Create Rollout.',
          'Select the Firmware Version to deploy (must be Approved).',
          'Choose the Target: All devices, by Group, by Site, or specific Device IDs.',
          'Select a Rollout Policy (defines batch size, retry count, timeout).',
          'Optionally schedule a Start Time — leave blank to start immediately.',
          'Click Save to create the rollout in Draft state.',
        ],
      },
      {
        title: 'Rollout Detail',
        description: 'Monitor rollout progress and manage execution.',
        screenshotDir: '07-ota-rollouts',
        screenshotPrefix: '03-rollout-detail',
        steps: [
          'Click a rollout name to open its detail page.',
          'The Summary section shows total devices, completed, failed, and in-progress counts.',
          'Click Start to begin execution (requires ReleaseManager role or above).',
          'Click Pause to halt execution temporarily.',
          'Click Resume to continue a paused rollout.',
          'Click Cancel to abort — this cannot be undone.',
        ],
      },
      {
        title: 'Device Job List',
        description: 'Per-device job status for this rollout.',
        screenshotDir: '07-ota-rollouts',
        screenshotPrefix: '04-rollout-job-list',
        steps: [
          'Scroll down on the rollout detail page to see the Jobs table.',
          'Each row represents a single device OTA job.',
          'Jobs can be retried individually if they failed.',
          'Click on a Device ID to navigate to that device\'s detail page.',
        ],
      },
    ],
  },
  {
    id: 'users',
    icon: '👥',
    title: 'User Management',
    intro: 'Administrators can manage platform users, assign roles, and control project-scope access. Available to SuperAdmin and PlatformAdmin.',
    subsections: [
      {
        title: 'Users List',
        description: 'View all users and their assigned roles.',
        screenshotDir: '08-users',
        screenshotPrefix: '01-users-list',
        steps: [
          'Navigate to Administration → Users.',
          'Search by name or email.',
          'Filter by Role using the dropdown.',
          'Status badges show Active or Deactivated.',
        ],
      },
      {
        title: 'Create User',
        description: 'Invite a new user and assign their role.',
        screenshotDir: '08-users',
        screenshotPrefix: '02-create-user-dialog',
        steps: [
          'Click Invite User / Create User.',
          'Enter Full Name and Email Address.',
          'Select the Role: SuperAdmin, PlatformAdmin, ReleaseManager, QA, CustomerAdmin, or Viewer.',
          'For CustomerAdmin, assign the Customer and optionally restrict Project Scope.',
          'Set a temporary password — the user must change it on first login.',
          'Click Save.',
        ],
      },
      {
        title: 'User Detail',
        description: 'View and edit user profile, role, and project scope.',
        screenshotDir: '08-users',
        screenshotPrefix: '03-user-detail',
        steps: [
          'Click on a user to open their detail page.',
          'Use Edit to change the full name.',
          'Use Assign Role to change the user\'s platform role.',
          'Use Deactivate to disable login without deleting the account.',
          'Deleted users are only possible for SuperAdmin.',
        ],
      },
    ],
  },
  {
    id: 'audit-logs',
    icon: '📋',
    title: 'Audit Logs',
    intro: 'Every create, update, delete, approve, and execute action is recorded in an immutable audit log for compliance and traceability.',
    subsections: [
      {
        title: 'Audit Logs List',
        description: 'Browse and filter the full audit trail.',
        screenshotDir: '09-audit-logs',
        screenshotPrefix: '01-audit-logs-list',
        steps: [
          'Navigate to Administration → Audit Logs.',
          'Filter by Date Range, User, Action Type, or Entity Type.',
          'Each entry shows: Timestamp, User, Action, Entity, and Detail.',
          'Click Export to download the logs as CSV or JSON for compliance reports.',
        ],
      },
    ],
  },
  {
    id: 'reports',
    icon: '📈',
    title: 'Reports',
    intro: 'The Reports section provides analytical insights across firmware, devices, rollouts, and users.',
    subsections: [
      {
        title: 'Firmware Approval Trends',
        description: 'Bar or line chart showing firmware approvals over time.',
        screenshotDir: '10-reports',
        screenshotPrefix: '01-firmware-trends',
        steps: [
          'Navigate to Administration → Reports → Firmware Trends.',
          'Select a date range to filter the trend data.',
          'The chart shows daily/weekly approval and rejection counts.',
        ],
      },
      {
        title: 'Rollout Success Rate',
        description: 'Success vs failure metrics for all rollouts.',
        screenshotDir: '10-reports',
        screenshotPrefix: '02-rollout-success',
        steps: [
          'Navigate to Reports → Rollout Success.',
          'Success rate = (Completed Jobs / Total Jobs) × 100.',
          'Export the data as CSV for external analysis.',
        ],
      },
      {
        title: 'Device Status Report',
        description: 'Pie chart breakdown of device states: Active, Suspended, Decommissioned.',
        screenshotDir: '10-reports',
        screenshotPrefix: '03-device-status',
        steps: [
          'Navigate to Reports → Device Status.',
          'The donut chart shows the proportion of devices in each state.',
          'Filter by Project to scope the report.',
        ],
      },
      {
        title: 'Daily OTA Progress',
        description: 'Day-by-day count of OTA jobs completed, failed, and pending.',
        screenshotDir: '10-reports',
        screenshotPrefix: '04-daily-ota-progress',
        steps: [
          'Navigate to Reports → Daily OTA Progress.',
          'Select a date range.',
          'The chart shows daily totals — useful for identifying deployment spikes.',
        ],
      },
      {
        title: 'Firmware Stage Distribution',
        description: 'Breakdown of firmware versions currently in each lifecycle stage.',
        screenshotDir: '10-reports',
        screenshotPrefix: '05-firmware-stage-distribution',
        steps: [
          'Navigate to Reports → Firmware Stage.',
          'The chart shows how many versions are in: Draft, Pending QA, Approved, Deprecated.',
        ],
      },
    ],
  },
  {
    id: 'webhook-events',
    icon: '🔗',
    title: 'Webhook Events',
    intro: 'Gitea webhook events are received and processed automatically. This view lets developers inspect event payloads and reprocess failed events.',
    subsections: [
      {
        title: 'Webhook Events List',
        description: 'View all received Gitea webhook events and their processing status.',
        screenshotDir: '11-webhook-events',
        screenshotPrefix: '01-webhook-events-list',
        steps: [
          'Navigate to Developer → Webhook Events.',
          'Filter by Status: Received, Processing, Processed, Failed.',
          'Click on an event to view the raw JSON payload.',
          'For Failed events, click Reprocess to retry event handling.',
        ],
      },
    ],
  },
  {
    id: 'settings',
    icon: '⚙️',
    title: 'Settings & Profile',
    intro: 'Personal and platform-level configuration settings.',
    subsections: [
      {
        title: 'Email Notifications',
        description: 'Control which email notifications you receive.',
        screenshotDir: '12-settings',
        screenshotPrefix: '01-email-notifications-settings',
        steps: [
          'Navigate to Administration → Settings → Email Notifications.',
          'Toggle on/off notifications for: Firmware Approved, Rollout Completed, Device Failures, etc.',
          'Click Save to apply your preferences.',
        ],
      },
      {
        title: 'Profile',
        description: 'View your profile and change your password.',
        screenshotDir: '12-settings',
        screenshotPrefix: '02-profile-page',
        steps: [
          'Click your name/avatar in the sidebar footer, or navigate to /profile.',
          'Your role, email, and linked Gitea profile are displayed.',
          'Use the Change Password section to update your password.',
          'Current Password is required to set a new password.',
        ],
      },
    ],
  },
]

// ─── Role Permission Table ────────────────────────────────────────────────────

const ROLE_TABLE_HTML = `
<table class="role-table">
  <thead>
    <tr>
      <th>Module</th>
      <th>SuperAdmin</th>
      <th>PlatformAdmin</th>
      <th>ReleaseManager</th>
      <th>QA</th>
      <th>CustomerAdmin</th>
      <th>Viewer</th>
    </tr>
  </thead>
  <tbody>
    <tr><td>Dashboard</td>      <td>✅ Full</td><td>✅ Full</td><td>👁️ View</td><td>👁️ View</td><td>👁️ View</td><td>👁️ View</td></tr>
    <tr><td>Projects</td>       <td>✅ Full</td><td>✅ No Delete</td><td>👁️ View</td><td>👁️ View</td><td>👁️ View</td><td>👁️ View</td></tr>
    <tr><td>Repositories</td>   <td>✅ Full</td><td>✅ No Delete</td><td>👁️ View</td><td>👁️ View</td><td>❌ None</td><td>👁️ View</td></tr>
    <tr><td>Firmware</td>       <td>✅ Full</td><td>✅ Full</td><td>✅ Approve</td><td>✅ QA Verify</td><td>👁️ View</td><td>👁️ View</td></tr>
    <tr><td>Devices</td>        <td>✅ Full</td><td>✅ Full</td><td>👁️ View</td><td>❌ None</td><td>👁️ View</td><td>👁️ View</td></tr>
    <tr><td>OTA Rollouts</td>   <td>✅ Full</td><td>✅ Full</td><td>✅ Execute</td><td>👁️ View</td><td>👁️ View</td><td>👁️ View</td></tr>
    <tr><td>Users</td>          <td>✅ Full</td><td>✅ No Delete</td><td>❌ None</td><td>❌ None</td><td>❌ None</td><td>❌ None</td></tr>
    <tr><td>Audit Logs</td>     <td>✅ Full</td><td>📤 Export</td><td>👁️ View</td><td>👁️ View</td><td>👁️ View</td><td>❌ None</td></tr>
    <tr><td>Reports</td>        <td>✅ Full</td><td>📤 Export</td><td>👁️ View</td><td>👁️ View</td><td>👁️ View</td><td>👁️ View</td></tr>
    <tr><td>Webhook Events</td> <td>✅ Full</td><td>✅ Reprocess</td><td>❌ None</td><td>❌ None</td><td>❌ None</td><td>❌ None</td></tr>
    <tr><td>System Settings</td><td>✅ Full</td><td>👁️ View</td><td>❌ None</td><td>❌ None</td><td>❌ None</td><td>❌ None</td></tr>
  </tbody>
</table>
`

// ─── HTML Template ────────────────────────────────────────────────────────────

function imageToBase64(imagePath: string): string {
  if (!fs.existsSync(imagePath)) return ''
  const data = fs.readFileSync(imagePath)
  return `data:image/png;base64,${data.toString('base64')}`
}

function findScreenshot(dir: string, prefix: string): string {
  const sectionDir = path.join(SCREENSHOTS_DIR, dir)
  if (!fs.existsSync(sectionDir)) return ''

  const files = fs.readdirSync(sectionDir).filter((f) => f.startsWith(prefix))
  if (files.length === 0) return ''

  return path.join(sectionDir, files[0])
}

function renderSubsection(sub: ManualSection['subsections'][0]): string {
  const screenshotPath = findScreenshot(sub.screenshotDir, sub.screenshotPrefix)
  const base64 = screenshotPath ? imageToBase64(screenshotPath) : ''

  const screenshotHtml = base64
    ? `<div class="screenshot-container">
         <img src="${base64}" alt="${sub.title}" class="screenshot" loading="lazy" />
         <p class="screenshot-caption">Screenshot: ${sub.title}</p>
       </div>`
    : `<div class="screenshot-placeholder">
         <p>📸 Screenshot not yet captured — run <code>npm run capture-screenshots</code></p>
       </div>`

  const stepsHtml = sub.steps && sub.steps.length > 0
    ? `<ol class="steps">${sub.steps.map((s) => `<li>${s}</li>`).join('\n')}</ol>`
    : ''

  return `
    <div class="subsection">
      <h3>${sub.title}</h3>
      <p class="subsection-desc">${sub.description}</p>
      ${stepsHtml}
      ${screenshotHtml}
    </div>`
}

function renderSection(section: ManualSection, index: number): string {
  const subsectionsHtml = section.subsections.map(renderSubsection).join('\n')

  return `
  <section id="${section.id}" class="section">
    <div class="section-header">
      <span class="section-icon">${section.icon}</span>
      <div>
        <span class="section-number">Chapter ${index + 1}</span>
        <h2>${section.title}</h2>
      </div>
    </div>
    <p class="section-intro">${section.intro}</p>
    ${subsectionsHtml}
  </section>`
}

function buildToc(): string {
  const items = MANUAL_SECTIONS.map((s, i) => `
    <li>
      <a href="#${s.id}">${s.icon} ${i + 1}. ${s.title}</a>
      <ul>${s.subsections.map((sub) => `<li><a href="#${s.id}">${sub.title}</a></li>`).join('')}</ul>
    </li>`).join('')

  return `<ul class="toc-list">${items}</ul>`
}

function buildHTML(): string {
  const sectionsHtml = MANUAL_SECTIONS.map((s, i) => renderSection(s, i)).join('\n')
  const toc = buildToc()

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>OTA Platform — User Manual</title>
  <style>
    /* ── Reset & Base ─────────────────────────────────────────────────── */
    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      font-size: 15px;
      line-height: 1.7;
      color: #1e293b;
      background: #f8fafc;
    }
    a { color: #0ea5e9; text-decoration: none; }
    a:hover { text-decoration: underline; }
    code {
      background: #f1f5f9;
      border: 1px solid #e2e8f0;
      border-radius: 4px;
      padding: 2px 6px;
      font-size: 13px;
      font-family: 'Menlo', 'Monaco', 'Courier New', monospace;
    }

    /* ── Layout ───────────────────────────────────────────────────────── */
    .wrapper { display: flex; min-height: 100vh; }

    /* ── Sidebar ToC ──────────────────────────────────────────────────── */
    .sidebar-toc {
      width: 280px;
      flex-shrink: 0;
      background: #0f172a;
      color: #94a3b8;
      position: sticky;
      top: 0;
      height: 100vh;
      overflow-y: auto;
      padding: 24px 0;
    }
    .sidebar-toc .logo {
      padding: 0 20px 20px;
      border-bottom: 1px solid #1e293b;
      margin-bottom: 16px;
    }
    .sidebar-toc .logo h1 {
      font-size: 16px;
      font-weight: 700;
      color: #f1f5f9;
      margin-bottom: 4px;
    }
    .sidebar-toc .logo p {
      font-size: 11px;
      color: #64748b;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .toc-list { list-style: none; padding: 0 12px; }
    .toc-list > li { margin-bottom: 2px; }
    .toc-list > li > a {
      display: block;
      padding: 7px 12px;
      border-radius: 6px;
      color: #94a3b8;
      font-size: 13px;
      font-weight: 500;
      transition: all 0.15s;
    }
    .toc-list > li > a:hover {
      background: #1e293b;
      color: #f1f5f9;
      text-decoration: none;
    }
    .toc-list > li > ul {
      list-style: none;
      padding: 2px 0 4px 28px;
    }
    .toc-list > li > ul li a {
      display: block;
      padding: 3px 8px;
      border-radius: 4px;
      color: #64748b;
      font-size: 12px;
      transition: color 0.15s;
    }
    .toc-list > li > ul li a:hover { color: #94a3b8; text-decoration: none; }

    /* ── Main Content ─────────────────────────────────────────────────── */
    .main-content { flex: 1; overflow-x: hidden; }

    /* ── Cover Page ───────────────────────────────────────────────────── */
    .cover {
      background: linear-gradient(135deg, #0f172a 0%, #1e3a5f 50%, #0c4a6e 100%);
      color: white;
      padding: 80px 60px;
      min-height: 400px;
      display: flex;
      flex-direction: column;
      justify-content: center;
    }
    .cover .badge {
      display: inline-block;
      background: rgba(14, 165, 233, 0.2);
      border: 1px solid rgba(14, 165, 233, 0.4);
      color: #7dd3fc;
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.1em;
      text-transform: uppercase;
      padding: 4px 12px;
      border-radius: 20px;
      margin-bottom: 20px;
    }
    .cover h1 {
      font-size: 42px;
      font-weight: 800;
      line-height: 1.2;
      margin-bottom: 12px;
      color: #f0f9ff;
    }
    .cover .subtitle {
      font-size: 18px;
      color: #93c5fd;
      margin-bottom: 32px;
    }
    .cover .meta {
      display: flex;
      gap: 32px;
      font-size: 13px;
      color: #64748b;
    }
    .cover .meta span { color: #94a3b8; }

    /* ── Section ──────────────────────────────────────────────────────── */
    .section {
      max-width: 900px;
      margin: 0 auto;
      padding: 48px 48px 64px;
      border-bottom: 1px solid #e2e8f0;
    }
    .section:last-child { border-bottom: none; }
    .section-header {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      margin-bottom: 20px;
    }
    .section-icon {
      font-size: 36px;
      line-height: 1;
      margin-top: 4px;
    }
    .section-number {
      display: block;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      color: #0ea5e9;
      margin-bottom: 4px;
    }
    .section h2 {
      font-size: 28px;
      font-weight: 700;
      color: #0f172a;
    }
    .section-intro {
      font-size: 15px;
      color: #475569;
      margin-bottom: 32px;
      padding: 16px 20px;
      background: #f0f9ff;
      border-left: 4px solid #0ea5e9;
      border-radius: 0 8px 8px 0;
    }

    /* ── Subsection ───────────────────────────────────────────────────── */
    .subsection {
      margin-bottom: 40px;
      background: white;
      border-radius: 12px;
      border: 1px solid #e2e8f0;
      overflow: hidden;
    }
    .subsection h3 {
      font-size: 17px;
      font-weight: 600;
      color: #1e293b;
      padding: 16px 20px;
      background: #f8fafc;
      border-bottom: 1px solid #e2e8f0;
    }
    .subsection-desc {
      padding: 12px 20px 4px;
      color: #64748b;
      font-size: 14px;
    }
    .steps {
      padding: 12px 20px 16px 36px;
      color: #475569;
      font-size: 14px;
    }
    .steps li {
      margin-bottom: 6px;
      padding-left: 4px;
    }
    .steps li::marker { color: #0ea5e9; font-weight: 600; }

    /* ── Screenshots ──────────────────────────────────────────────────── */
    .screenshot-container {
      padding: 16px 20px 20px;
      border-top: 1px solid #f1f5f9;
    }
    .screenshot {
      width: 100%;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      box-shadow: 0 4px 16px rgba(0,0,0,0.08);
      display: block;
    }
    .screenshot-caption {
      font-size: 12px;
      color: #94a3b8;
      text-align: center;
      margin-top: 8px;
      font-style: italic;
    }
    .screenshot-placeholder {
      padding: 24px 20px;
      background: #fffbeb;
      border-top: 1px solid #fde68a;
      text-align: center;
      color: #92400e;
      font-size: 13px;
    }

    /* ── Role Table ───────────────────────────────────────────────────── */
    .role-section {
      max-width: 900px;
      margin: 0 auto;
      padding: 48px;
    }
    .role-section h2 {
      font-size: 24px;
      font-weight: 700;
      margin-bottom: 16px;
      color: #0f172a;
    }
    .role-section p {
      color: #64748b;
      margin-bottom: 24px;
    }
    .role-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 13px;
    }
    .role-table th {
      background: #0f172a;
      color: white;
      padding: 10px 12px;
      text-align: left;
      font-weight: 600;
    }
    .role-table td {
      padding: 9px 12px;
      border-bottom: 1px solid #e2e8f0;
      color: #475569;
    }
    .role-table tr:nth-child(even) td { background: #f8fafc; }
    .role-table tr:hover td { background: #f0f9ff; }
    .role-table td:first-child { font-weight: 500; color: #1e293b; }

    /* ── Firmware Lifecycle ───────────────────────────────────────────── */
    .lifecycle {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
      padding: 20px 48px;
      background: white;
      border-bottom: 1px solid #e2e8f0;
    }
    .lifecycle-step {
      padding: 6px 14px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
    }
    .lifecycle-arrow { color: #94a3b8; font-size: 16px; }
    .status-draft      { background: #f1f5f9; color: #475569; border: 1px solid #cbd5e1; }
    .status-pendingqa  { background: #fef9c3; color: #854d0e; border: 1px solid #fde047; }
    .status-qaverified { background: #dcfce7; color: #166534; border: 1px solid #86efac; }
    .status-pendingapp { background: #dbeafe; color: #1e40af; border: 1px solid #93c5fd; }
    .status-approved   { background: #dcfce7; color: #166534; border: 1px solid #86efac; }
    .status-rejected   { background: #fee2e2; color: #991b1b; border: 1px solid #fca5a5; }
    .status-deprecated { background: #f1f5f9; color: #64748b; border: 1px solid #cbd5e1; }

    /* ── Footer ───────────────────────────────────────────────────────── */
    .footer {
      background: #0f172a;
      color: #64748b;
      padding: 24px 48px;
      font-size: 12px;
      text-align: center;
    }
    .footer a { color: #0ea5e9; }

    @media print {
      .sidebar-toc { display: none; }
      .section { padding: 32px; }
      .screenshot { max-height: 500px; object-fit: contain; }
    }
  </style>
</head>
<body>
  <div class="wrapper">
    <!-- ── Sidebar Table of Contents ────────────────────────────────────── -->
    <nav class="sidebar-toc">
      <div class="logo">
        <h1>OTA Platform</h1>
        <p>User Manual</p>
      </div>
      ${toc}
    </nav>

    <!-- ── Main Content ─────────────────────────────────────────────────── -->
    <div class="main-content">

      <!-- Cover -->
      <div class="cover">
        <span class="badge">Enterprise Firmware Update Management</span>
        <h1>OTA Platform<br/>User Manual</h1>
        <p class="subtitle">Complete guide for operating the OTA firmware update platform</p>
        <div class="meta">
          <div>📅 Generated: <span>${GENERATED_DATE}</span></div>
          <div>📖 Version: <span>1.0</span></div>
          <div>🏢 Confidential</div>
        </div>
      </div>

      <!-- Firmware Lifecycle Banner -->
      <div class="lifecycle">
        <strong style="margin-right:8px;color:#475569;font-size:13px;">Firmware Lifecycle:</strong>
        <span class="lifecycle-step status-draft">Draft</span>
        <span class="lifecycle-arrow">→</span>
        <span class="lifecycle-step status-pendingqa">Pending QA</span>
        <span class="lifecycle-arrow">→</span>
        <span class="lifecycle-step status-qaverified">QA Verified</span>
        <span class="lifecycle-arrow">→</span>
        <span class="lifecycle-step status-pendingapp">Pending Approval</span>
        <span class="lifecycle-arrow">→</span>
        <span class="lifecycle-step status-approved">Approved</span>
        <span class="lifecycle-arrow">/</span>
        <span class="lifecycle-step status-rejected">Rejected</span>
        <span class="lifecycle-arrow">→</span>
        <span class="lifecycle-step status-deprecated">Deprecated</span>
      </div>

      <!-- Role & Permission Overview -->
      <div class="role-section">
        <h2>🔐 Role &amp; Permission Overview</h2>
        <p>The OTA Platform uses Role-Based Access Control (RBAC). Your role determines which modules and actions you can access.</p>
        ${ROLE_TABLE_HTML}
      </div>

      <!-- Sections -->
      ${sectionsHtml}

      <!-- Footer -->
      <div class="footer">
        <p>OTA Platform User Manual &mdash; Generated on ${GENERATED_DATE}</p>
        <p>Enterprise Firmware Update Management &bull; All sessions are monitored and audited.</p>
      </div>
    </div>
  </div>
</body>
</html>`
}

// ─── Main ─────────────────────────────────────────────────────────────────────

function main() {
  console.log('🔨 OTA Platform — User Manual Generator')
  console.log(`📁 Screenshots dir : ${SCREENSHOTS_DIR}`)
  console.log(`📄 Output file     : ${OUTPUT_FILE}`)
  console.log()

  // Ensure output dir exists
  const outputDir = path.dirname(OUTPUT_FILE)
  if (!fs.existsSync(outputDir)) fs.mkdirSync(outputDir, { recursive: true })

  // Count available screenshots
  let screenshotCount = 0
  if (fs.existsSync(SCREENSHOTS_DIR)) {
    const walk = (dir: string) => {
      fs.readdirSync(dir, { withFileTypes: true }).forEach((entry) => {
        if (entry.isDirectory()) walk(path.join(dir, entry.name))
        else if (entry.name.endsWith('.png')) screenshotCount++
      })
    }
    walk(SCREENSHOTS_DIR)
  }
  console.log(`📸 Found ${screenshotCount} screenshots`)

  const html = buildHTML()
  fs.writeFileSync(OUTPUT_FILE, html, 'utf8')

  const fileSizeKb = Math.round(fs.statSync(OUTPUT_FILE).size / 1024)
  console.log()
  console.log(`✅ Manual generated: ${OUTPUT_FILE}`)
  console.log(`   Size: ${fileSizeKb} KB`)
  console.log()
  console.log('💡 Open the file in a browser to view the user manual.')
  console.log('   To regenerate after new screenshots: npm run generate-manual')
}

main()
