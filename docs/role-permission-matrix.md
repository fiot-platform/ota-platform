# OTA Platform — Role × Permission Matrix

## Legend

| Symbol | Meaning |
|--------|---------|
| **V** | View / Read |
| **C** | Create |
| **U** | Update / Edit |
| **A** | Approve (workflow approval action) |
| **D** | Delete / Deactivate |
| **E** | Execute (trigger a job, rollout, sync, etc.) |
| **X** | Export (download CSV / PDF report) |
| ——   | No access |

---

## Role × Module × Permission Matrix

| Role | Dashboard | Users | Projects | Repositories | Firmware | Devices | OTA Rollouts | Audit Logs | Reports | Webhook Events | Settings |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **SuperAdmin** | V,X | V,C,U,D | V,C,U,D | V,C,U,D,E | V,C,U,A,D,E | V,C,U,D | V,C,U,A,D,E | V,X | V,X | V,C,U,D,E | V,C,U,D |
| **PlatformAdmin** | V,X | V,C,U,D | V,C,U,D | V,C,U,D,E | V,C,U,D | V,C,U,D | V,C,U,D,E | V,X | V,X | V,U,D,E | V,C,U |
| **ReleaseManager** | V | — | V | V | V,C,U,A,D | V | V,C,U,A,E | V | V,X | V | — |
| **QA** | V | — | V | V | V,U | V | V | V | V | V | — |
| **DevOpsEngineer** | V | — | V | V,C,U,E | V | V,C,U | V,C,U,E | V | V | V,E | V |
| **SupportEngineer** | V | — | V | V | V | V,U | V | V | V | V | — |
| **CustomerAdmin** | V,X | V,C,U | V | V | V | V,C,U,D | V,C,U | V | V,X | — | — |
| **Viewer** | V | — | V | V | V | V | V | — | V | — | — |
| **Auditor** | V | V | V | V | V | V | V | V,X | V,X | V | — |
| **Device** | — | — | — | — | V | — | — | — | — | — | — |

---

## Detailed Permission Breakdown per Role

### SuperAdmin
- **Dashboard**: Full view + export of all KPIs across all customers and projects.
- **Users**: Create, view, update, and hard-delete any user across all tenants; assign any role.
- **Projects**: Full CRUD over all projects globally.
- **Repositories**: Full CRUD + trigger manual sync with Gitea.
- **Firmware**: Full lifecycle management including approve/reject; can deprecate any firmware.
- **Devices**: Full CRUD; can decommission and suspend devices.
- **OTA Rollouts**: Create, schedule, approve, pause, cancel, and force-complete rollouts.
- **Audit Logs**: Read all logs globally and export to CSV/JSON.
- **Reports**: Generate and export all report types.
- **Webhook Events**: Full CRUD on webhook configurations; retry failed events.
- **Settings**: Manage all platform settings including JWT secrets, Gitea integration, policies.

### PlatformAdmin
- **Dashboard**: Full view + export.
- **Users**: Manage users within their tenancy scope; cannot assign SuperAdmin role.
- **Projects**: Full CRUD within scope.
- **Repositories**: CRUD + manual Gitea sync.
- **Firmware**: CRUD; cannot perform final approval (that belongs to ReleaseManager/SuperAdmin).
- **Devices**: CRUD; can suspend and decommission.
- **OTA Rollouts**: Full operational control except workflow approval.
- **Audit Logs**: Read + export all logs.
- **Reports**: Full report access + export.
- **Webhook Events**: View + retry + delete failed events.
- **Settings**: View and update most platform settings (not secret rotation).

### ReleaseManager
- **Dashboard**: View only.
- **Projects**: View all projects in scope.
- **Repositories**: View repository list and details.
- **Firmware**: Create new firmware entries, update metadata, **approve or reject** firmware from PendingApproval state, deprecate old versions. Cannot delete firmware once approved.
- **Devices**: View device list and current firmware status.
- **OTA Rollouts**: Create, update, **approve** rollout execution, and trigger start/pause/cancel.
- **Audit Logs**: View logs for traceability.
- **Reports**: View + export firmware approval trend and rollout success rate reports.

### QA Engineer
- **Dashboard**: View only.
- **Projects**: View list.
- **Repositories**: View list and webhook events.
- **Firmware**: View firmware detail and update QA verification status (mark as QAVerified or request re-submission). Cannot approve for production.
- **Devices**: View list.
- **OTA Rollouts**: View rollout status and job-level results.
- **Audit Logs**: View logs relevant to QA actions.
- **Reports**: View firmware and device status reports.

### DevOps Engineer
- **Dashboard**: View operational KPIs.
- **Projects**: View list.
- **Repositories**: Register new repositories, update metadata, trigger Gitea sync.
- **Firmware**: View firmware catalogue.
- **Devices**: Register new devices, update device metadata and site assignment.
- **OTA Rollouts**: Create and manage rollouts; trigger execution; cannot approve firmware.
- **Audit Logs**: View all logs.
- **Reports**: View operational reports.
- **Webhook Events**: Retry failed webhook events.
- **Settings**: View integration settings.

### Support Engineer
- **Dashboard**: View key health metrics.
- **Projects**: View list.
- **Repositories**: View list.
- **Firmware**: View firmware catalogue.
- **Devices**: View and update device metadata (tags, site assignment); cannot register or delete.
- **OTA Rollouts**: View rollout and job status; cannot create or trigger.
- **Audit Logs**: View logs for troubleshooting.
- **Reports**: View device update status reports.

### CustomerAdmin
- **Dashboard**: View + export their customer-scoped KPIs.
- **Users**: Create, view, and update users within their customer tenant; cannot assign roles above CustomerAdmin.
- **Projects**: View their customer's projects.
- **Repositories**: View repositories linked to their projects.
- **Firmware**: View firmware catalogue applicable to their devices.
- **Devices**: Full CRUD on their device fleet; can decommission own devices.
- **OTA Rollouts**: Create, view, and update rollouts for their devices; cannot approve firmware.
- **Audit Logs**: View audit logs for their tenant only.
- **Reports**: View + export reports scoped to their customer.

### Viewer
- **Dashboard**: Read-only view of all dashboards visible to their assigned scope.
- **Projects**: View project list and details.
- **Repositories**: View repository list and details.
- **Firmware**: View firmware catalogue.
- **Devices**: View device list and details.
- **OTA Rollouts**: View rollout status and progress.
- **Reports**: View reports (no export).
- All other modules: No access.

### Auditor
- **All modules**: Read-only access across all modules in scope.
- **Audit Logs**: Full read + export; this is the Auditor's primary function.
- **Reports**: Full read + export for compliance reporting.
- Cannot create, update, approve, delete, or execute anything.

### Device (Machine Identity)
- **Firmware**: Can call check-update endpoint to receive firmware metadata (download URL, SHA256, version). Read-only, device-scoped.
- All other modules: No access. Device identity tokens are scoped to a single `DeviceId` claim.

---

## Notes

1. All permissions are additive within a role; there is no permission inheritance between roles.
2. `CustomerAdmin` users are always scoped to their `CustomerId`; they cannot view or affect other customers' data.
3. `ReleaseManager` approval and `QA` verification are sequential gates enforced at the application layer.
4. `SuperAdmin` audit log access is immutable and cannot be restricted by any policy.
5. `Device` role tokens are short-lived (1-hour expiry) and issued exclusively by the device registration flow.
6. Dual-approval policy (if enabled on a `RolloutPolicy`) requires two distinct `ReleaseManager` or higher users to approve before a rollout transitions to Active.
