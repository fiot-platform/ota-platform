# ============================================================
# OTA Platform API – Multi-stage Dockerfile
# Stage 1: Build  → restore, compile, publish
# Stage 2: Runtime → minimal aspnet image, EXPOSE 8080
# ============================================================

# ── Stage 1: Build ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first to take advantage of Docker layer caching.
# If csproj hasn't changed the restore layer is reused.
COPY ["OTA.API/OTA.API.csproj", "OTA.API/"]
RUN dotnet restore "OTA.API/OTA.API.csproj" --no-cache

# Copy the remaining source files.
COPY . .

WORKDIR /src/OTA.API

# Publish a self-contained, Release-optimised build to /app/publish.
RUN dotnet publish "OTA.API.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ── Stage 2: Runtime ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a non-root user for security hardening.
RUN adduser --disabled-password --gecos "" --no-create-home appuser \
    && chown -R appuser:appuser /app

# Copy published output from build stage.
COPY --from=build --chown=appuser:appuser /app/publish .

# Create log directory writable by the application user.
RUN mkdir -p /app/logs && chown appuser:appuser /app/logs

USER appuser

# ASP.NET Core listens on port 8080 by default in the aspnet:8.0 image.
EXPOSE 8080

# Environment defaults (override at runtime with -e or docker-compose env_file).
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Health check – calls the /health endpoint exposed by MapHealthChecks.
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "OTA.API.dll"]
