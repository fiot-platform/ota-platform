// ─── Enums ────────────────────────────────────────────────────────────────────

export enum UserRole {
  SuperAdmin = 'SuperAdmin',
  PlatformAdmin = 'PlatformAdmin',
  ReleaseManager = 'ReleaseManager',
  QA = 'QA',
  DevOpsEngineer = 'DevOpsEngineer',
  SupportEngineer = 'SupportEngineer',
  CustomerAdmin = 'CustomerAdmin',
  Viewer = 'Viewer',
  Auditor = 'Auditor',
  Device = 'Device',
}

export enum FirmwareStatus {
  Draft = 'Draft',
  PendingQA = 'PendingQA',
  QAVerified = 'QAVerified',
  PendingApproval = 'PendingApproval',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Deprecated = 'Deprecated',
  Active = 'Active',
}

export enum FirmwareChannel {
  Alpha = 'Alpha',
  Beta = 'Beta',
  Staging = 'Staging',
  Production = 'Production',
}

export enum DeviceStatus {
  Active = 'Active',
  Inactive = 'Inactive',
  Suspended = 'Suspended',
  Decommissioned = 'Decommissioned',
  Pending = 'Pending',
}

export enum RolloutStatus {
  Draft = 'Draft',
  Scheduled = 'Scheduled',
  InProgress = 'InProgress',
  Paused = 'Paused',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  Failed = 'Failed',
}

export enum OtaJobStatus {
  Pending = 'Pending',
  Queued = 'Queued',
  InProgress = 'InProgress',
  Succeeded = 'Succeeded',
  Failed = 'Failed',
  Skipped = 'Skipped',
  Cancelled = 'Cancelled',
  Retrying = 'Retrying',
}

export enum AuditAction {
  UserCreated = 'UserCreated',
  UserUpdated = 'UserUpdated',
  UserDeleted = 'UserDeleted',
  UserLogin = 'UserLogin',
  UserLogout = 'UserLogout',
  ProjectCreated = 'ProjectCreated',
  ProjectUpdated = 'ProjectUpdated',
  ProjectActivated = 'ProjectActivated',
  ProjectDeactivated = 'ProjectDeactivated',
  RepositoryRegistered = 'RepositoryRegistered',
  RepositorySynced = 'RepositorySynced',
  FirmwareCreated = 'FirmwareCreated',
  FirmwareApproved = 'FirmwareApproved',
  FirmwareRejected = 'FirmwareRejected',
  FirmwareQAVerified = 'FirmwareQAVerified',
  FirmwareDeprecated = 'FirmwareDeprecated',
  FirmwareChannelAssigned = 'FirmwareChannelAssigned',
  RolloutCreated = 'RolloutCreated',
  RolloutStarted = 'RolloutStarted',
  RolloutPaused = 'RolloutPaused',
  RolloutResumed = 'RolloutResumed',
  RolloutCancelled = 'RolloutCancelled',
  OtaJobRetried = 'OtaJobRetried',
  DeviceRegistered = 'DeviceRegistered',
  DeviceSuspended = 'DeviceSuspended',
  DeviceDecommissioned = 'DeviceDecommissioned',
  RoleAssigned = 'RoleAssigned',
  SystemConfigChanged = 'SystemConfigChanged',
  WebhookEventReceived = 'WebhookEventReceived',
  WebhookEventReprocessed = 'WebhookEventReprocessed',
}

export enum WebhookEventStatus {
  Received = 'Received',
  Processing = 'Processing',
  Processed = 'Processed',
  Failed = 'Failed',
  Skipped = 'Skipped',
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string
  password: string
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: User
}

export interface JwtPayload {
  sub: string
  userId: string
  email: string
  role: UserRole
  fullName?: string
  customerId?: string
  projectScope?: string[]
  iat: number
  exp: number
}

// ─── User ─────────────────────────────────────────────────────────────────────

export interface User {
  id: string
  name: string
  email: string
  role: UserRole
  customerId?: string
  customerName?: string
  projectScope?: string[]
  isActive: boolean
  createdAt: string
  updatedAt: string
  lastLoginAt?: string
}

export interface CreateUserRequest {
  name: string
  email: string
  password: string
  role: UserRole
  customerId?: string
  projectScope?: string[]
  isActive: boolean
}

export interface UpdateUserRequest {
  name?: string
  email?: string
  role?: UserRole
  customerId?: string
  projectScope?: string[]
  isActive?: boolean
}

// ─── Project ──────────────────────────────────────────────────────────────────

export interface Project {
  id: string
  name: string
  description?: string
  customerId: string
  customerName: string
  businessUnit?: string
  giteaOrgName?: string
  tags?: string[]
  isActive: boolean
  createdAt: string
  updatedAt: string
  repositoryCount?: number
  firmwareCount?: number
}

export interface CreateProjectRequest {
  name: string
  description?: string
  customerId: string
  customerName: string
  businessUnit?: string
  giteaOrgName?: string
  tags?: string[]
}

export interface UpdateProjectRequest {
  name?: string
  description?: string
  customerName?: string
  businessUnit?: string
  giteaOrgName?: string
  tags?: string[]
}

// ─── Repository ───────────────────────────────────────────────────────────────

export interface Repository {
  id: string
  name: string
  giteaOwner: string
  giteaRepo: string
  giteaUrl?: string
  projectId: string
  projectName?: string
  description?: string
  defaultBranch: string
  isActive: boolean
  lastSyncedAt?: string
  webhookConfigured: boolean
  createdAt: string
  updatedAt: string
}

export interface RegisterRepositoryRequest {
  name: string
  giteaOwner: string
  giteaRepo: string
  giteaUrl?: string
  projectId: string
  description?: string
  defaultBranch?: string
}

// ─── Firmware ─────────────────────────────────────────────────────────────────

export interface GiteaAsset {
  id: string
  name: string
  downloadUrl: string
  size: number
  contentType: string
}

export interface FirmwareVersion {
  id: string
  firmwareId?: string
  version: string
  repositoryId: string
  repositoryName?: string
  projectId: string
  projectName?: string
  channel: FirmwareChannel
  status: FirmwareStatus
  releaseNotes?: string
  checksum?: string
  fileSha256?: string
  fileSizeBytes?: number
  fileName?: string
  downloadUrl?: string
  giteaTagName?: string
  giteaReleaseId?: number
  giteaAssets?: GiteaAsset[]
  isMandate?: boolean
  supportedModels?: string[]
  supportedHardwareRevisions?: string[]
  minRequiredVersion?: string
  maxAllowedVersion?: string
  approvedBy?: string
  approvedAt?: string
  approvalNotes?: string
  rejectedBy?: string
  rejectedAt?: string
  rejectionReason?: string
  qaVerifiedBy?: string
  qaVerifiedAt?: string
  qaRemarks?: string
  isQaVerified: boolean
  createdAt: string
  updatedAt: string
  createdByUserId?: string
}

export interface CreateFirmwareRequest {
  repositoryId: string
  version: string
  giteaTagName?: string
  channel: FirmwareChannel
  releaseNotes?: string
  fileName?: string
  fileSha256?: string
  fileSizeBytes?: number
  downloadUrl?: string
  isMandate?: boolean
  minRequiredVersion?: string
  maxAllowedVersion?: string
  supportedModels?: string[]
  supportedHardwareRevisions?: string[]
}

export interface UpdateFirmwareRequest {
  releaseNotes?: string
  isMandate?: boolean
  supportedModels?: string[]
  supportedHardwareRevisions?: string[]
  minRequiredVersion?: string
  maxAllowedVersion?: string
}

export interface ApproveFirmwareRequest {
  approvalNotes?: string
}

export interface RejectFirmwareRequest {
  rejectionReason: string
}

export interface QAVerifyRequest {
  qaRemarks?: string
}

export interface AssignChannelRequest {
  channel: FirmwareChannel
}

// ─── Device ───────────────────────────────────────────────────────────────────

export interface Device {
  id: string
  serialNumber: string
  model: string
  hardwareRevision?: string
  firmwareVersionId?: string
  currentFirmwareVersion?: string
  customerId: string
  customerName?: string
  siteId?: string
  siteName?: string
  status: DeviceStatus
  lastHeartbeatAt?: string
  registeredAt: string
  updatedAt: string
  ipAddress?: string
  macAddress?: string
  metadata?: Record<string, string>
}

export interface RegisterDeviceRequest {
  serialNumber: string
  model: string
  hardwareRevision?: string
  customerId: string
  siteId?: string
  metadata?: Record<string, string>
}

export interface CheckUpdateRequest {
  currentFirmwareVersion: string
  channel: FirmwareChannel
  projectId: string
}

export interface CheckUpdateResponse {
  hasUpdate: boolean
  firmwareVersion?: FirmwareVersion
  downloadUrl?: string
}

export interface UpdateDeviceRequest {
  model?: string
  hardwareRevision?: string
  siteId?: string
  metadata?: Record<string, string>
}

// ─── OTA Rollout ──────────────────────────────────────────────────────────────

export enum RolloutTargetType {
  AllDevices = 'AllDevices',
  DeviceGroup = 'DeviceGroup',
  Site = 'Site',
  Channel = 'Channel',
  SpecificDevices = 'SpecificDevices',
}

export interface Rollout {
  id: string
  name: string
  description?: string
  firmwareVersionId: string
  firmwareVersion?: string
  projectId: string
  projectName?: string
  status: RolloutStatus
  targetType: RolloutTargetType
  targetIds?: string[]
  policyId?: string
  policyName?: string
  scheduledAt?: string
  startedAt?: string
  completedAt?: string
  totalDevices: number
  pendingCount: number
  inProgressCount: number
  succeededCount: number
  failedCount: number
  skippedCount: number
  cancelledCount: number
  createdBy: string
  createdAt: string
  updatedAt: string
}

export interface CreateRolloutRequest {
  name: string
  description?: string
  firmwareVersionId: string
  projectId: string
  targetType: RolloutTargetType
  targetIds?: string[]
  policyId?: string
  scheduledAt?: string
}

export interface OtaJob {
  id: string
  rolloutId: string
  deviceId: string
  deviceSerialNumber?: string
  deviceModel?: string
  status: OtaJobStatus
  attemptCount: number
  maxAttempts: number
  lastAttemptAt?: string
  completedAt?: string
  failureReason?: string
  downloadProgress?: number
  createdAt: string
  updatedAt: string
}

export interface RolloutSummary {
  rolloutId: string
  name: string
  status: RolloutStatus
  totalDevices: number
  succeededCount: number
  failedCount: number
  pendingCount: number
  inProgressCount: number
  skippedCount: number
  successRate: number
  jobStatusBreakdown: { status: OtaJobStatus; count: number }[]
}

// ─── Rollout Policy ───────────────────────────────────────────────────────────

export interface RolloutPolicy {
  id: string
  name: string
  description?: string
  maxParallelUpdates: number
  retryCount: number
  retryIntervalSeconds: number
  timeoutSeconds: number
  rollbackOnFailure: boolean
  failureThresholdPercent: number
  createdAt: string
  updatedAt: string
}

export interface CreatePolicyRequest {
  name: string
  description?: string
  maxParallelUpdates: number
  retryCount: number
  retryIntervalSeconds: number
  timeoutSeconds: number
  rollbackOnFailure: boolean
  failureThresholdPercent: number
}

// ─── Audit Log ────────────────────────────────────────────────────────────────

export interface AuditLog {
  id: string
  action: AuditAction | string
  performedBy: string
  performedByName?: string
  performedByRole?: UserRole
  entityType?: string
  entityId?: string
  entityName?: string
  ipAddress?: string
  userAgent?: string
  details?: Record<string, unknown>
  timestamp: string
  customerId?: string
}

export interface AuditLogFilter {
  action?: string
  performedBy?: string
  entityType?: string
  entityId?: string
  customerId?: string
  startDate?: string
  endDate?: string
  page?: number
  pageSize?: number
}

// ─── Webhook Event ────────────────────────────────────────────────────────────

export interface WebhookEvent {
  id: string
  giteaRepo: string
  giteaOwner: string
  eventType: string
  status: WebhookEventStatus
  payload?: Record<string, unknown>
  errorMessage?: string
  retryCount: number
  maxRetries: number
  receivedAt: string
  processedAt?: string
  repositoryId?: string
}

// ─── Reports / Dashboard ──────────────────────────────────────────────────────

export interface DashboardSummary {
  totalProjects: number
  totalRepositories: number
  totalFirmwareVersions: number
  activeDevices: number
  activeRollouts: number
  pendingApprovals: number
  recentAuditEvents: AuditLog[]
  deviceStatusBreakdown: { status: DeviceStatus; count: number }[]
  rolloutStatusBreakdown: { status: RolloutStatus; count: number }[]
}

export interface FirmwareApprovalTrend {
  date: string
  approved: number
  rejected: number
  submitted: number
}

export interface RolloutSuccessRate {
  projectId: string
  projectName: string
  totalRollouts: number
  successfulRollouts: number
  failedRollouts: number
  successRate: number
}

export interface DeviceUpdateStatus {
  customerId?: string
  customerName?: string
  upToDate: number
  updateAvailable: number
  updating: number
  failed: number
  offline: number
  total: number
}

// ─── Pagination / API Response ────────────────────────────────────────────────

export interface PaginationInfo {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface ApiResponse<T> {
  data: T
  success: boolean
  message?: string
  errors?: string[]
  pagination?: PaginationInfo
  timestamp?: string
}

export interface PaginatedResponse<T> {
  items: T[]
  pagination: PaginationInfo
}

// ─── Filter Types ─────────────────────────────────────────────────────────────

export interface UserFilters {
  role?: UserRole
  customerId?: string
  isActive?: boolean
  search?: string
  page?: number
  pageSize?: number
}

export interface DeviceFilters {
  customerId?: string
  siteId?: string
  status?: DeviceStatus
  model?: string
  search?: string
  page?: number
  pageSize?: number
}

export interface FirmwareFilters {
  status?: FirmwareStatus
  channel?: FirmwareChannel
  projectId?: string
  repositoryId?: string
  search?: string
  page?: number
  pageSize?: number
}

export interface RolloutFilters {
  status?: RolloutStatus
  projectId?: string
  firmwareVersionId?: string
  search?: string
  page?: number
  pageSize?: number
}

// ── QA Session Types ──────────────────────────────────────────────────────────

export enum QASessionStatus {
  NotStarted   = 'NotStarted',
  InProgress   = 'InProgress',
  BugListRaised = 'BugListRaised',
  Complete     = 'Complete',
  Fail         = 'Fail',
}

export enum BugSeverity {
  Low      = 'Low',
  Medium   = 'Medium',
  High     = 'High',
  Critical = 'Critical',
}

export enum BugStatus {
  Open       = 'Open',
  InProgress = 'InProgress',
  Resolved   = 'Resolved',
  WontFix    = 'WontFix',
}

export interface QADocumentItem {
  documentId: string
  name: string
  storedFileName: string
  downloadUrl: string
  fileSizeBytes: number
  uploadedAt: string
  uploadedByUserId: string
}

export interface QABugItem {
  bugId: string
  title: string
  description?: string
  severity: BugSeverity
  bugStatus: BugStatus
  reportedAt: string
  reportedByUserId: string
  resolvedAt?: string
  resolution?: string
}

export interface QAEventLogItem {
  eventId: string
  eventType: string
  description: string
  userId: string
  timestamp: string
  metadata?: Record<string, string>
}

export interface QASession {
  sessionId: string
  firmwareId: string
  status: QASessionStatus
  testCaseDocuments: QADocumentItem[]
  testResultDocuments: QADocumentItem[]
  bugs: QABugItem[]
  eventLog: QAEventLogItem[]
  startedAt?: string
  startedByUserId?: string
  completedAt?: string
  remarks?: string
  createdAt: string
  updatedAt: string
  totalBugs: number
  openBugs: number
  resolvedBugs: number
  totalTestCaseDocs: number
  totalTestResultDocs: number
}

export interface AddBugRequest {
  title: string
  description?: string
  severity: BugSeverity
}

export interface UpdateBugRequest {
  title?: string
  description?: string
  severity?: BugSeverity
  bugStatus?: BugStatus
  resolution?: string
}

export interface UpdateQAStatusRequest {
  status: QASessionStatus
  remarks?: string
}

export interface CompleteQARequest {
  finalStatus: QASessionStatus
  remarks?: string
}

export interface WebhookEventFilters {
  status?: WebhookEventStatus
  eventType?: string
  giteaRepo?: string
  page?: number
  pageSize?: number
}
