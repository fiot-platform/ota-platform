namespace OTA.API.Models.Enums
{
    /// <summary>
    /// Defines all platform user roles used for RBAC enforcement across API endpoints and UI screens.
    /// </summary>
    public enum UserRole
    {
        /// <summary>Full platform access across all tenants, settings, and lifecycle operations.</summary>
        SuperAdmin = 1,

        /// <summary>Administrative access scoped to an assigned set of customers and projects.</summary>
        PlatformAdmin = 2,

        /// <summary>Manages firmware release approvals and OTA rollout scheduling.</summary>
        ReleaseManager = 3,

        /// <summary>Performs QA verification of firmware builds before they enter the approval queue.</summary>
        QA = 4,

        /// <summary>Registers repositories, manages devices, and executes rollout operations.</summary>
        DevOpsEngineer = 5,

        /// <summary>Read-heavy role for troubleshooting device and rollout issues.</summary>
        SupportEngineer = 6,

        /// <summary>Customer-tenant administrator managing their own users, devices, and rollouts.</summary>
        CustomerAdmin = 7,

        /// <summary>Read-only access to dashboards, firmware catalogue, and rollout status.</summary>
        Viewer = 8,

        /// <summary>Compliance role with read + export access to all audit logs and reports.</summary>
        Auditor = 9,

        /// <summary>Machine identity issued to registered IoT devices for check-update and report-status calls.</summary>
        Device = 10
    }

    /// <summary>
    /// Lifecycle states of a firmware version from creation through approval to deprecation.
    /// </summary>
    public enum FirmwareStatus
    {
        /// <summary>Firmware has been created (via webhook or manually) but not yet submitted for QA.</summary>
        Draft = 1,

        /// <summary>Firmware has been submitted to the QA team for verification.</summary>
        PendingQA = 2,

        /// <summary>QA team has verified the firmware is ready for release manager approval.</summary>
        QAVerified = 3,

        /// <summary>Firmware has passed QA and is awaiting release manager final approval.</summary>
        PendingApproval = 4,

        /// <summary>Firmware has been approved for OTA deployment to devices.</summary>
        Approved = 5,

        /// <summary>Firmware was rejected at QA or approval stage and cannot be deployed.</summary>
        Rejected = 6,

        /// <summary>Firmware was previously approved but has been superseded; new rollouts cannot use it.</summary>
        Deprecated = 7
    }

    /// <summary>
    /// Distribution channels controlling which device populations receive firmware builds.
    /// </summary>
    public enum FirmwareChannel
    {
        /// <summary>Early access; deployed only to developer or internal alpha devices.</summary>
        Alpha = 1,

        /// <summary>Beta testing group; limited external device pool.</summary>
        Beta = 2,

        /// <summary>Pre-production staging environment for final integration testing.</summary>
        Staging = 3,

        /// <summary>General availability; deployed to all production devices matching the firmware target.</summary>
        Production = 4
    }

    /// <summary>
    /// Operational states of an individual OTA update job assigned to a single device.
    /// </summary>
    public enum OtaJobStatus
    {
        /// <summary>Job record has been created but not yet placed in the execution queue.</summary>
        Created = 1,
        /// <summary>Alias for Created — job is pending dispatch.</summary>
        Pending = 1,

        /// <summary>Job is in the execution queue awaiting device acknowledgement.</summary>
        Queued = 2,

        /// <summary>Device has acknowledged the job and firmware download/installation is in progress.</summary>
        InProgress = 3,

        /// <summary>Device has successfully installed the firmware and reported success.</summary>
        Succeeded = 4,

        /// <summary>Firmware installation failed on the device; retry logic applies per policy.</summary>
        Failed = 5,

        /// <summary>Job was cancelled by an operator before or during execution.</summary>
        Cancelled = 6,

        /// <summary>Job execution has been paused (e.g., parent rollout paused); will resume on rollout resume.</summary>
        Paused = 7
    }

    /// <summary>
    /// Operational states of a registered IoT device in the platform.
    /// </summary>
    public enum DeviceStatus
    {
        /// <summary>Device is registered, reachable, and eligible to receive OTA updates.</summary>
        Active = 1,

        /// <summary>Device has not sent a heartbeat within the configured threshold; treated as offline.</summary>
        Inactive = 2,

        /// <summary>Device has been administratively suspended; OTA jobs will not be dispatched.</summary>
        Suspended = 3,

        /// <summary>Device has been permanently decommissioned; historical records are retained.</summary>
        Decommissioned = 4
    }

    /// <summary>
    /// High-level lifecycle states of an OTA rollout campaign.
    /// </summary>
    public enum RolloutStatus
    {
        /// <summary>Rollout is being configured and has not yet been scheduled or started.</summary>
        Draft = 1,

        /// <summary>Rollout is configured and has a future start time; jobs will be created at scheduled time.</summary>
        Scheduled = 2,

        /// <summary>Rollout is actively dispatching OTA jobs to target devices.</summary>
        Active = 3,

        /// <summary>Rollout execution has been paused by an operator; in-flight jobs continue to their terminal state.</summary>
        Paused = 4,

        /// <summary>All target devices have reached a terminal job status (succeeded or failed); rollout is closed.</summary>
        Completed = 5,

        /// <summary>Rollout was cancelled by an operator before completion; pending jobs are cancelled.</summary>
        Cancelled = 6,

        /// <summary>Rollout encountered an unrecoverable error (e.g., firmware deprecated mid-rollout).</summary>
        Failed = 7
    }

    /// <summary>
    /// Processing states of an inbound Gitea webhook event.
    /// </summary>
    public enum WebhookEventStatus
    {
        /// <summary>Webhook payload has been received and persisted but not yet processed.</summary>
        Received = 1,

        /// <summary>Background processor is currently handling the event.</summary>
        Processing = 2,

        /// <summary>Event has been successfully processed and all downstream effects applied.</summary>
        Processed = 3,

        /// <summary>Processing failed after all retry attempts are exhausted.</summary>
        Failed = 4,

        /// <summary>Processing failed and the event is scheduled for an automatic retry.</summary>
        Retrying = 5,

        /// <summary>Event type was recognised but no action was required; intentionally skipped.</summary>
        Skipped = 6
    }

    /// <summary>
    /// All auditable actions that produce an immutable AuditLog entry.
    /// </summary>
    public enum AuditAction
    {
        // User management
        UserCreated = 1,
        UserUpdated = 2,
        UserDeactivated = 3,
        UserRoleAssigned = 4,
        UserPasswordReset = 5,
        UserLoggedIn = 6,
        UserLoggedOut = 7,
        UserActivated = 8,
        UserRoleChanged = 4,  // alias — same value as UserRoleAssigned
        PasswordChanged = 5,  // alias — same value as UserPasswordReset

        // Firmware lifecycle
        FirmwareCreated = 10,
        FirmwareUpdated = 11,
        FirmwareSubmittedForQA = 12,
        FirmwareQAVerified = 13,
        FirmwareQARejected = 14,
        FirmwareSubmittedForApproval = 15,
        FirmwareApproved = 16,
        FirmwareRejected = 17,
        FirmwareDeprecated = 18,
        FirmwareChannelAssigned = 19,

        // Rollout lifecycle
        RolloutCreated = 20,
        RolloutScheduled = 21,
        RolloutStarted = 22,
        RolloutPaused = 23,
        RolloutResumed = 24,
        RolloutCancelled = 25,
        RolloutCompleted = 26,
        RolloutFailed = 27,

        // OTA Job
        OtaJobCreated = 30,
        OtaJobQueued = 31,
        OtaJobStarted = 32,
        OtaJobSucceeded = 33,
        OtaJobFailed = 34,
        OtaJobCancelled = 35,
        OtaJobRetried = 36,

        // Device management
        DeviceRegistered = 40,
        DeviceUpdated = 41,
        DeviceSuspended = 42,
        DeviceReactivated = 43,
        DeviceDecommissioned = 44,
        DeviceHeartbeat = 45,
        DeviceFirmwareUpdated = 46,

        // Repository & webhook
        RepositoryRegistered = 50,
        RepositoryUpdated = 51,
        RepositorySynced = 52,
        RepositoryDeactivated = 53,
        WebhookReceived = 54,
        WebhookProcessed = 55,
        WebhookFailed = 56,
        WebhookRetried = 57,

        // Project management
        ProjectCreated = 60,
        ProjectUpdated = 61,
        ProjectDeactivated = 62,
        ProjectActivated = 61,  // alias — same value as ProjectUpdated

        // Policy management
        PolicyCreated = 70,
        PolicyUpdated = 71,
        PolicyDeactivated = 72,
        RolloutPolicyCreated = 70,   // alias — same value as PolicyCreated
        RolloutPolicyUpdated = 71,   // alias — same value as PolicyUpdated
        RolloutPolicyDeleted = 72    // alias — same value as PolicyDeactivated
    }

    /// <summary>
    /// Specifies whether a firmware update must be applied or can be deferred by the device.
    /// </summary>
    public enum UpdateType
    {
        /// <summary>Device must apply the update; no deferral is permitted.</summary>
        Mandatory = 1,

        /// <summary>Device may defer the update according to its local policy.</summary>
        Optional = 2
    }

    /// <summary>
    /// Defines how the set of target devices for a rollout is resolved.
    /// </summary>
    public enum TargetType
    {
        /// <summary>All devices registered in the project or customer scope.</summary>
        AllDevices = 1,

        /// <summary>A named group of devices (DeviceGroup entity).</summary>
        DeviceGroup = 2,

        /// <summary>All devices assigned to one or more physical sites.</summary>
        Site = 3,

        /// <summary>All devices subscribed to a specific firmware channel (e.g., Beta).</summary>
        Channel = 4,

        /// <summary>An explicit list of individual DeviceId values.</summary>
        SpecificDevices = 5
    }

    /// <summary>
    /// Types of Gitea webhook events that the platform can receive and process.
    /// </summary>
    public enum GiteaEventType
    {
        /// <summary>A commit was pushed to a branch.</summary>
        Push = 1,

        /// <summary>A new release was published.</summary>
        Release = 2,

        /// <summary>A new Git tag was created.</summary>
        Tag = 3,

        /// <summary>A new branch or tag reference was created.</summary>
        Create = 4,

        /// <summary>A branch or tag reference was deleted.</summary>
        Delete = 5,

        /// <summary>A pull request event was received.</summary>
        PullRequest = 6,

        /// <summary>An issue event was received.</summary>
        Issues = 7,

        /// <summary>An unrecognised or unsupported event type.</summary>
        Unknown = 99
    }

    /// <summary>
    /// Execution strategy for how OTA jobs are distributed across the target device population.
    /// </summary>
    public enum RolloutPhase
    {
        /// <summary>Deploy to a small percentage of devices first (CanaryPercentage) to validate stability.</summary>
        Canary = 1,

        /// <summary>Deploy in sequential batches of fixed size (RollingBatchSize) with health checks between batches.</summary>
        Rolling = 2,

        /// <summary>Deploy to all target devices simultaneously.</summary>
        Full = 3
    }
}
