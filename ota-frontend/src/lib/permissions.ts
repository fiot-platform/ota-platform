import { UserRole } from '@/types'

// ─── Permission Matrix ────────────────────────────────────────────────────────

export type PermissionAction = 'view' | 'create' | 'update' | 'delete' | 'approve' | 'execute' | 'export'

export type PermissionModule =
  | 'Dashboard'
  | 'Projects'
  | 'Repositories'
  | 'Firmware'
  | 'Devices'
  | 'OtaRollouts'
  | 'Users'
  | 'AuditLogs'
  | 'Reports'
  | 'WebhookEvents'
  | 'RolloutPolicies'
  | 'SystemSettings'

export type RolePermissions = {
  [module in PermissionModule]: {
    [action in PermissionAction]: boolean
  }
}

const noAccess: { [action in PermissionAction]: boolean } = {
  view: false,
  create: false,
  update: false,
  delete: false,
  approve: false,
  execute: false,
  export: false,
}

const readOnly: { [action in PermissionAction]: boolean } = {
  view: true,
  create: false,
  update: false,
  delete: false,
  approve: false,
  execute: false,
  export: false,
}

const fullAccess: { [action in PermissionAction]: boolean } = {
  view: true,
  create: true,
  update: true,
  delete: true,
  approve: true,
  execute: true,
  export: true,
}

export const PERMISSION_MATRIX: Record<UserRole, RolePermissions> = {
  [UserRole.SuperAdmin]: {
    Dashboard: fullAccess,
    Projects: fullAccess,
    Repositories: fullAccess,
    Firmware: fullAccess,
    Devices: fullAccess,
    OtaRollouts: fullAccess,
    Users: fullAccess,
    AuditLogs: fullAccess,
    Reports: fullAccess,
    WebhookEvents: fullAccess,
    RolloutPolicies: fullAccess,
    SystemSettings: fullAccess,
  },

  [UserRole.PlatformAdmin]: {
    Dashboard: fullAccess,
    Projects: { ...fullAccess, delete: false },
    Repositories: { ...fullAccess, delete: false },
    Firmware: fullAccess,
    Devices: fullAccess,
    OtaRollouts: fullAccess,
    Users: { view: true, create: true, update: true, delete: false, approve: false, execute: false, export: true },
    AuditLogs: { ...readOnly, export: true },
    Reports: { ...readOnly, export: true },
    WebhookEvents: { view: true, create: false, update: true, delete: false, approve: false, execute: true, export: false },
    RolloutPolicies: { ...fullAccess, delete: false },
    SystemSettings: { ...readOnly, update: false, delete: false, approve: false, execute: false, export: false },
  },

  [UserRole.ReleaseManager]: {
    Dashboard: readOnly,
    Projects: readOnly,
    Repositories: readOnly,
    Firmware: {
      view: true,
      create: true,
      update: true,
      delete: false,
      approve: true,
      execute: false,
      export: false,
    },
    Devices: readOnly,
    OtaRollouts: {
      view: true,
      create: true,
      update: true,
      delete: false,
      approve: false,
      execute: true,
      export: false,
    },
    Users: noAccess,
    AuditLogs: readOnly,
    Reports: readOnly,
    WebhookEvents: noAccess,
    RolloutPolicies: readOnly,
    SystemSettings: noAccess,
  },

  [UserRole.QA]: {
    Dashboard: readOnly,
    Projects: readOnly,
    Repositories: readOnly,
    Firmware: {
      view: true,
      create: false,
      update: false,
      delete: false,
      approve: true,
      execute: false,
      export: false,
    },
    Devices: readOnly,
    OtaRollouts: readOnly,
    Users: noAccess,
    AuditLogs: readOnly,
    Reports: readOnly,
    WebhookEvents: noAccess,
    RolloutPolicies: readOnly,
    SystemSettings: noAccess,
  },

  [UserRole.DevOpsEngineer]: {
    Dashboard: readOnly,
    Projects: readOnly,
    Repositories: {
      view: true,
      create: false,
      update: false,
      delete: false,
      approve: false,
      execute: true,
      export: false,
    },
    Firmware: readOnly,
    Devices: readOnly,
    OtaRollouts: readOnly,
    Users: noAccess,
    AuditLogs: readOnly,
    Reports: readOnly,
    WebhookEvents: {
      view: true,
      create: false,
      update: false,
      delete: false,
      approve: false,
      execute: true,
      export: false,
    },
    RolloutPolicies: readOnly,
    SystemSettings: readOnly,
  },

  [UserRole.SupportEngineer]: {
    Dashboard: readOnly,
    Projects: readOnly,
    Repositories: noAccess,
    Firmware: readOnly,
    Devices: readOnly,
    OtaRollouts: {
      view: true,
      create: false,
      update: false,
      delete: false,
      approve: false,
      execute: true,
      export: false,
    },
    Users: noAccess,
    AuditLogs: readOnly,
    Reports: readOnly,
    WebhookEvents: noAccess,
    RolloutPolicies: readOnly,
    SystemSettings: noAccess,
  },

  [UserRole.CustomerAdmin]: {
    Dashboard: readOnly,
    Projects: readOnly,
    Repositories: noAccess,
    Firmware: readOnly,
    Devices: readOnly,
    OtaRollouts: readOnly,
    Users: noAccess,
    AuditLogs: { ...readOnly, export: false },
    Reports: readOnly,
    WebhookEvents: noAccess,
    RolloutPolicies: noAccess,
    SystemSettings: noAccess,
  },

  [UserRole.Viewer]: {
    Dashboard: readOnly,
    Projects: readOnly,
    Repositories: readOnly,
    Firmware: readOnly,
    Devices: readOnly,
    OtaRollouts: readOnly,
    Users: noAccess,
    AuditLogs: noAccess,
    Reports: readOnly,
    WebhookEvents: noAccess,
    RolloutPolicies: readOnly,
    SystemSettings: noAccess,
  },

  [UserRole.Auditor]: {
    Dashboard: { ...noAccess, view: true },
    Projects: noAccess,
    Repositories: noAccess,
    Firmware: noAccess,
    Devices: noAccess,
    OtaRollouts: noAccess,
    Users: noAccess,
    AuditLogs: { view: true, create: false, update: false, delete: false, approve: false, execute: false, export: true },
    Reports: { view: true, create: false, update: false, delete: false, approve: false, execute: false, export: true },
    WebhookEvents: noAccess,
    RolloutPolicies: noAccess,
    SystemSettings: noAccess,
  },

  [UserRole.Device]: {
    Dashboard: noAccess,
    Projects: noAccess,
    Repositories: noAccess,
    Firmware: noAccess,
    Devices: noAccess,
    OtaRollouts: noAccess,
    Users: noAccess,
    AuditLogs: noAccess,
    Reports: noAccess,
    WebhookEvents: noAccess,
    RolloutPolicies: noAccess,
    SystemSettings: noAccess,
  },
}

// ─── Helper Function ──────────────────────────────────────────────────────────

export function canAccess(
  role: UserRole | undefined | null,
  module: PermissionModule,
  action: PermissionAction
): boolean {
  if (!role) return false
  const rolePermissions = PERMISSION_MATRIX[role]
  if (!rolePermissions) return false
  const modulePermissions = rolePermissions[module]
  if (!modulePermissions) return false
  return modulePermissions[action] === true
}

export function getAccessibleModules(role: UserRole): PermissionModule[] {
  const permissions = PERMISSION_MATRIX[role]
  return (Object.keys(permissions) as PermissionModule[]).filter(
    (module) => permissions[module].view
  )
}
