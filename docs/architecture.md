# OTA Platform — Enterprise Architecture Document

## 1. System Overview

The OTA (Over-The-Air) Firmware Update Platform is an enterprise-grade, cloud-native solution designed to manage the full lifecycle of firmware releases across tens of thousands of IoT/embedded devices. The platform integrates with Gitea as its source of truth for firmware binary artefacts and release metadata, exposing a RESTful ASP.NET Core API as its central orchestration layer. All persistent state—user identities, device registries, firmware catalogue, rollout schedules, audit trails, and webhook events—is stored in MongoDB. A React/Next.js single-page application provides a role-aware dashboard for platform operators, release managers, QA engineers, and customer administrators. The system enforces a strict approval workflow (QA verification → dual approval) before any firmware build may be deployed to production devices, and it provides granular, role-based access control (RBAC) across every API surface and UI screen.

---

## 2. Component Responsibilities

### 2.1 Gitea (Self-Hosted Git & Release Server)
- Hosts all firmware source repositories organised under Gitea organisations (one per customer/project).
- Produces Release artefacts (binary `.bin`/`.tar.gz` files) tagged with semantic versions.
- Fires webhook events (Push, Tag, Release, Create, Delete) to the OTA API on repository activity.
- Acts as the canonical download source for firmware binaries; devices receive pre-signed or proxied Gitea asset URLs.

### 2.2 ASP.NET Core API (OTA.API)
- Implements Clean Architecture with Domain, Application, Infrastructure, and Presentation layers.
- Orchestrates the firmware approval workflow: Draft → PendingQA → QAVerified → PendingApproval → Approved/Rejected.
- Manages rollout scheduling, device targeting, batch execution (canary / rolling / full), and real-time status aggregation.
- Consumes Gitea webhooks and reconciles release metadata into the firmware catalogue.
- Enforces JWT-based authentication and RBAC policy on every endpoint.
- Persists all state to MongoDB via the Repository pattern.
- Emits structured audit log entries for every mutating operation.

### 2.3 MongoDB (Primary Data Store)
- Stores all platform entities: Users, Projects, Repositories, FirmwareVersions, Devices, OtaJobs, Rollouts, RolloutPolicies, RepositoryEvents (webhook), and AuditLogs.
- Provides compound indexes for high-throughput device check-update queries (DeviceId + FirmwareVersion + Status).
- Supports aggregation pipelines for dashboard KPIs and trend reports.
- Collections are logically namespaced per customer (`CustomerId`) to support multi-tenancy.

### 2.4 React / Next.js Frontend
- Server-side rendered (SSR) dashboard for fast initial load and SEO-safe public pages.
- Role-aware UI: menu items, action buttons, and data columns are conditionally rendered per the caller's `UserRole`.
- Integrates with the OTA API using typed Axios/fetch clients generated from OpenAPI specifications.
- Provides real-time rollout progress via WebSocket or polling.
- Supports dark mode, accessibility (WCAG 2.1 AA), and responsive layout for operations centres.

---

## 3. Clean Architecture Layer Diagram

```mermaid
graph TD
    subgraph Presentation["Presentation Layer"]
        A1[REST Controllers]
        A2[Middleware: JWT / RBAC / Error]
        A3[SignalR Hubs]
    end
    subgraph Application["Application Layer"]
        B1[Use Cases / Command Handlers]
        B2[Query Handlers]
        B3[Validators - FluentValidation]
        B4[MediatR Pipeline]
    end
    subgraph Domain["Domain Layer"]
        C1[Entities & Enums]
        C2[Domain Events]
        C3[Business Rules / Specifications]
        C4[Repository Interfaces]
    end
    subgraph Infrastructure["Infrastructure Layer"]
        D1[MongoDB Repositories]
        D2[Gitea HTTP Client]
        D3[JWT Token Service]
        D4[Audit Log Service]
        D5[Background Job Service]
    end

    A1 --> B4
    A2 --> A1
    A3 --> B2
    B4 --> B1
    B4 --> B2
    B1 --> C4
    B2 --> C4
    B1 --> C2
    C4 --> D1
    D1 --> D4
    B1 --> D2
    B1 --> D3
```

---

## 4. Mermaid Diagrams

### 4a. Overall OTA Platform Workflow

```mermaid
flowchart TD
    Dev["Developer\npushes code / tag"] --> Gitea["Gitea\nRepository"]
    Gitea -->|Release / Tag webhook| API["OTA API\n(ASP.NET Core)"]
    API -->|Persist event| Mongo["MongoDB"]
    API -->|Create FirmwareVersion Draft| Mongo
    QA["QA Engineer"] -->|Verify firmware| API
    API -->|Status → QAVerified| Mongo
    RM["Release Manager"] -->|Approve firmware| API
    API -->|Status → Approved| Mongo
    Ops["DevOps / Platform Admin"] -->|Create & Schedule Rollout| API
    API -->|Create OTA Jobs per device| Mongo
    Device["IoT Device"] -->|Check for update| API
    API -->|Return FW download URL| Device
    Device -->|Download binary from| Gitea
    Device -->|Report update status| API
    API -->|Update OtaJob & Device record| Mongo
    UI["React/Next.js\nDashboard"] -->|Read KPIs / Logs| API
```

### 4b. Gitea Webhook Flow

```mermaid
sequenceDiagram
    participant G as Gitea
    participant API as OTA API
    participant DB as MongoDB

    G->>API: POST /api/webhooks/gitea (HMAC-signed payload)
    API->>API: Validate HMAC signature
    API->>DB: Insert RepositoryEvent {Status=Received}
    API-->>G: 200 OK (fast ack)
    API->>API: Background: parse GiteaEventType
    alt Release event
        API->>DB: Upsert FirmwareVersion {Status=Draft}
        API->>DB: Update RepositoryEvent {Status=Processed}
    else Tag / Push event
        API->>DB: Update RepositoryEvent {Status=Processed}
    else Processing error
        API->>DB: Update RepositoryEvent {Status=Failed, RetryCount++}
    end
```

### 4c. Firmware Approval Flow

```mermaid
stateDiagram-v2
    [*] --> Draft : Webhook / Manual create
    Draft --> PendingQA : Submit for QA
    PendingQA --> QAVerified : QA Engineer verifies
    PendingQA --> Rejected : QA Engineer rejects
    QAVerified --> PendingApproval : Request approval
    PendingApproval --> Approved : Release Manager approves\n(dual approval if policy)
    PendingApproval --> Rejected : Release Manager rejects
    Approved --> Deprecated : Superseded by newer version
    Rejected --> Draft : Re-submit after fix
    Deprecated --> [*]
```

### 4d. Device Check-Update Flow

```mermaid
sequenceDiagram
    participant D as IoT Device
    participant API as OTA API
    participant DB as MongoDB

    D->>API: POST /api/devices/check-update\n{DeviceId, CurrentVersion, Model, HWRev}
    API->>DB: Find active OtaJob for DeviceId
    alt Active job exists
        DB-->>API: OtaJob {FirmwareId, Status=Queued}
        API->>DB: Find FirmwareVersion {Status=Approved}
        DB-->>API: FirmwareVersion {DownloadUrl, Sha256, ...}
        API-->>D: 200 {HasUpdate=true, FirmwareVersion, DownloadUrl, Sha256, IsMandatory}
    else No active job
        API-->>D: 200 {HasUpdate=false}
    end
```

### 4e. Rollout Execution Flow

```mermaid
flowchart TD
    Start([Rollout Scheduled / Started]) --> Resolve["Resolve target device list\nfrom TargetType + TargetIds"]
    Resolve --> Policy["Apply RolloutPolicy\n(MaxConcurrent, Phase, BatchSize)"]
    Policy --> Phase{RolloutPhase}
    Phase -->|Canary| Canary["Select CanaryPercentage %\nof devices → create OtaJobs"]
    Phase -->|Rolling| Rolling["Create jobs in batches\nof RollingBatchSize"]
    Phase -->|Full| Full["Create all OtaJobs\nin one pass"]
    Canary --> Monitor["Monitor job statuses\nvia background service"]
    Rolling --> Monitor
    Full --> Monitor
    Monitor --> Check{All succeeded?}
    Check -->|Yes| Complete([Rollout Completed])
    Check -->|Partial failure| Retry["Retry up to RetryLimit\nper RolloutPolicy"]
    Retry --> Monitor
    Check -->|Paused by operator| Pause([Rollout Paused])
    Check -->|Cancelled by operator| Cancel([Rollout Cancelled])
```

### 4f. Device Report-Status Flow

```mermaid
sequenceDiagram
    participant D as IoT Device
    participant API as OTA API
    participant DB as MongoDB

    D->>API: POST /api/devices/report-status\n{JobId, Status, ErrorMessage}
    API->>DB: Find OtaJob by JobId
    DB-->>API: OtaJob record
    API->>DB: Update OtaJob.Status
    alt Status = Succeeded
        API->>DB: Update Device.CurrentFirmwareVersion
        API->>DB: Increment Rollout.SucceededCount
    else Status = Failed
        API->>DB: Increment Rollout.FailedCount
        alt AttemptCount < RetryLimit
            API->>DB: Requeue OtaJob {Status=Queued, AttemptCount++}
        end
    end
    API->>DB: Insert AuditLog {Action=DeviceHeartbeat}
    API-->>D: 200 OK
```

### 4g. Frontend–Backend–Gitea–MongoDB Architecture

```mermaid
flowchart LR
    subgraph Client["Browser / Mobile"]
        UI["React / Next.js\nDashboard"]
    end
    subgraph Backend["OTA API (ASP.NET Core)"]
        Auth["Auth Middleware\n(JWT + RBAC)"]
        Controllers["REST Controllers"]
        AppLayer["Application Layer\n(MediatR + Use Cases)"]
        InfraLayer["Infrastructure Layer\n(Repos + Clients)"]
    end
    subgraph Data["Data & External"]
        Mongo[("MongoDB")]
        GiteaSvr["Gitea Server"]
    end

    UI -->|HTTPS REST + JWT| Auth
    Auth --> Controllers
    Controllers --> AppLayer
    AppLayer --> InfraLayer
    InfraLayer -->|MongoDB Driver| Mongo
    InfraLayer -->|Gitea REST API| GiteaSvr
    GiteaSvr -->|Webhook POST| Controllers
```

### 4h. Role-Based Access Flow

```mermaid
flowchart TD
    Request["Incoming API Request\n(JWT Bearer Token)"] --> JWTMiddleware["JWT Validation Middleware"]
    JWTMiddleware -->|Valid| RoleExtract["Extract UserRole + CustomerId\nfrom JWT Claims"]
    JWTMiddleware -->|Invalid / Expired| Reject401["401 Unauthorized"]
    RoleExtract --> RBACPolicy["RBAC Policy Handler\n[Authorize(Policy=...)]"]
    RBACPolicy -->|Role sufficient| ResourceScope["Resource Scope Check\n(CustomerId / ProjectScope)"]
    RBACPolicy -->|Role insufficient| Reject403["403 Forbidden"]
    ResourceScope -->|In scope| Handler["Controller / Use Case Handler"]
    ResourceScope -->|Out of scope| Reject403
    Handler --> AuditLog["Emit AuditLog Entry\n(Action, UserId, EntityId)"]
    AuditLog --> Response["200 / 201 Response"]
```

---

## 5. Technology Stack Summary

| Layer | Technology |
|---|---|
| Frontend | React 18, Next.js 14 (App Router), TypeScript, Tailwind CSS |
| API | ASP.NET Core 8, MediatR, FluentValidation, AutoMapper |
| Auth | JWT Bearer, Refresh Tokens, BCrypt password hashing |
| Database | MongoDB 7 (replica set), MongoDB .NET Driver 3 |
| Source Control | Gitea (self-hosted) |
| Containerisation | Docker, Docker Compose |
| Reverse Proxy | NGINX |
| CI/CD | Gitea Actions / GitHub Actions |
| Monitoring | Seq (structured logging), Prometheus + Grafana |

---

## 6. Security Considerations

- All inter-service communication uses TLS 1.3.
- Gitea webhook payloads are HMAC-SHA256 validated before processing.
- Firmware binary integrity is enforced via SHA-256 checksum comparison on device.
- JWT tokens have short expiry (15 min); refresh tokens are rotated on each use and stored hashed in MongoDB.
- MongoDB collections are access-controlled via dedicated service accounts; no direct internet access.
- All mutating operations produce immutable audit log entries with IP address and user agent.
- Dual-approval policy is enforced at the application layer for production firmware promotion.
