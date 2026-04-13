# OTA Platform — Setup & Deployment Guide

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| Node.js | 20 LTS |
| MongoDB | 6.0+ (replica set) |
| Gitea | 1.20+ |
| Docker (optional) | 24+ |

---

## MongoDB

The platform connects to:

```
mongodb://iot.ssmsportal.com:39999/?replicaSet=fiot-rs
```

This is already configured in `OTA.API/appsettings.json` and `appsettings.Development.json`.

> **Note:** The connection uses the `fiot-rs` replica set. Ensure your application server can reach `iot.ssmsportal.com:39999`. If authentication is required, add credentials to the connection string:
> ```
> mongodb://username:password@iot.ssmsportal.com:39999/?replicaSet=fiot-rs&authSource=admin
> ```

### Collections Auto-Created

All MongoDB collections and indexes are created automatically at startup by the repository constructors. No manual migration is needed.

Collections:
- `Users` — unique index on `Email`
- `Projects` — index on `CustomerId`, `IsActive`
- `Repositories` — unique index on `GiteaRepoId`
- `FirmwareVersions` — compound index on `(RepositoryId, Status, Channel)`
- `Devices` — unique index on `SerialNumber`
- `OtaJobs` — index on `(RolloutId, Status)`, `DeviceId`
- `Rollouts` — index on `(ProjectId, Status)`
- `RepositoryEvents` — index on `(Status, ReceivedAt)`
- `AuditLogs` — index on `(Timestamp DESC, Action)`
- `RolloutPolicies` — index on `IsActive`

---

## Backend Setup

```bash
cd OTAPlatform/OTA.API

# Restore packages
dotnet restore

# Set secrets (or edit appsettings.Development.json)
dotnet user-secrets set "JwtSettings:SecretKey" "your-256-bit-secret"
dotnet user-secrets set "GiteaSettings:AdminToken" "your-gitea-token"
dotnet user-secrets set "GiteaSettings:WebhookSecret" "your-webhook-secret"

# Run in development
dotnet run --environment Development

# API available at:
# http://localhost:5000
# Swagger UI: http://localhost:5000/swagger
```

### Seed SuperAdmin User

On first run, create the first SuperAdmin via the API:

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@ota.local","password":"Admin@123!"}'
```

> The first SuperAdmin must be seeded directly in MongoDB or via a seed endpoint if added.

---

## Frontend Setup

```bash
cd OTAPlatform/ota-frontend

# Copy env file
cp .env.example .env.local

# Edit .env.local
# NEXT_PUBLIC_API_URL=http://localhost:5000/api
# NEXTAUTH_SECRET=your-secret
# NEXTAUTH_URL=http://localhost:3000

# Install dependencies
npm install

# Run dev server
npm run dev

# App available at:
# http://localhost:3000
```

---

## Docker Compose (Full Stack)

```yaml
# docker-compose.yml  (place at OTAPlatform root)
version: "3.9"

services:
  ota-api:
    build:
      context: ./OTA.API
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - MongoDbSettings__ConnectionString=mongodb://iot.ssmsportal.com:39999/?replicaSet=fiot-rs
      - MongoDbSettings__DatabaseName=OTAPlatform
      - JwtSettings__SecretKey=${JWT_SECRET}
      - GiteaSettings__BaseUrl=${GITEA_BASE_URL}
      - GiteaSettings__AdminToken=${GITEA_ADMIN_TOKEN}
      - GiteaSettings__WebhookSecret=${GITEA_WEBHOOK_SECRET}
      - CorsSettings__AllowedOrigins__0=http://localhost:3000
    restart: unless-stopped

  ota-frontend:
    build:
      context: ./ota-frontend
    ports:
      - "3000:3000"
    environment:
      - NEXT_PUBLIC_API_URL=http://ota-api:8080/api
      - NEXTAUTH_SECRET=${NEXTAUTH_SECRET}
      - NEXTAUTH_URL=http://localhost:3000
    depends_on:
      - ota-api
    restart: unless-stopped
```

```bash
# From OTAPlatform root
cp OTA.API/.env.example .env
# Fill in .env values, then:
docker compose up -d
```

---

## Gitea Webhook Configuration

1. In your Gitea repository go to **Settings → Webhooks → Add Webhook → Gitea**
2. Set:
   - **Target URL:** `https://your-api-host/api/webhooks/gitea`
   - **Secret:** same value as `GiteaSettings:WebhookSecret`
   - **Trigger:** Release, Push, Tag, Create events
3. Click **Test Delivery** to verify

---

## Environment Variables Reference

| Variable | Description |
|----------|-------------|
| `MongoDbSettings__ConnectionString` | MongoDB replica set URI |
| `MongoDbSettings__DatabaseName` | Database name (`OTAPlatform`) |
| `JwtSettings__SecretKey` | HMAC-SHA256 signing key (≥ 32 chars) |
| `JwtSettings__Issuer` | JWT issuer claim |
| `JwtSettings__Audience` | JWT audience claim |
| `JwtSettings__AccessTokenExpiryMinutes` | Access token TTL |
| `JwtSettings__RefreshTokenExpiryDays` | Refresh token TTL |
| `GiteaSettings__BaseUrl` | Gitea instance base URL |
| `GiteaSettings__AdminToken` | Gitea admin API token |
| `GiteaSettings__WebhookSecret` | HMAC secret for webhook validation |
| `CorsSettings__AllowedOrigins__0` | Frontend URL for CORS |

---

## Health Check

```
GET /health
```

Returns `200 OK` with MongoDB and Gitea connectivity status.

---

## API Documentation

Swagger UI is available at `/swagger` in non-Production environments.

All endpoints follow `api/[controller]` routing with JWT Bearer authentication.

See `docs/architecture.md` for full workflow diagrams and `docs/role-permission-matrix.md` for access control details.
