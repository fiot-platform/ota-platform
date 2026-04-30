#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Deploys OTA Platform API to IIS.
    Creates app pool "OTAPlatform-API" and site "OTAPlatform-API" on port 5000.
#>

$ErrorActionPreference = 'Stop'
$appPoolName  = 'OTAPlatform-API'
$siteName     = 'OTAPlatform-API'
$physicalPath = 'C:\inetpub\wwwroot\ota-api'
$port         = 5000
$appUser      = 'IIS AppPool\OTAPlatform-API'

Import-Module WebAdministration -ErrorAction Stop

Write-Host "=== OTA Platform API — IIS Deployment ===" -ForegroundColor Cyan

# ── 1. Create Logs and upload dirs ──────────────────────────────────────────
foreach ($dir in @("$physicalPath\Logs", "$physicalPath\firmware-uploads", "$physicalPath\qa-docs")) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "Created: $dir"
    }
}

# ── 2. Application Pool ──────────────────────────────────────────────────────
if (Get-WebConfiguration "system.applicationHost/applicationPools/add[@name='$appPoolName']") {
    Write-Host "App pool '$appPoolName' already exists — updating..."
    $pool = Get-Item "IIS:\AppPools\$appPoolName"
} else {
    Write-Host "Creating app pool '$appPoolName'..."
    New-WebAppPool -Name $appPoolName | Out-Null
    $pool = Get-Item "IIS:\AppPools\$appPoolName"
}

# ASP.NET Core requires No Managed Code
$pool.managedRuntimeVersion = ''
$pool.managedPipelineMode   = 'Integrated'
$pool.startMode             = 'AlwaysRunning'
$pool.autoStart             = $true
$pool.processModel.idleTimeout = [TimeSpan]::FromMinutes(0)   # never idle stop
$pool.recycling.periodicRestart.time = [TimeSpan]::FromHours(0) # no scheduled recycle
$pool | Set-Item
Write-Host "App pool configured (No Managed Code, Always Running)." -ForegroundColor Green

# ── 3. IIS Site ──────────────────────────────────────────────────────────────
if (Get-WebSite -Name $siteName -ErrorAction SilentlyContinue) {
    Write-Host "Site '$siteName' already exists — updating binding..."
    Set-ItemProperty "IIS:\Sites\$siteName" -Name physicalPath -Value $physicalPath
    # Update binding to port $port
    Get-WebBinding -Name $siteName | Remove-WebBinding
    New-WebBinding -Name $siteName -Protocol "http" -Port $port -IPAddress "*"
} else {
    Write-Host "Creating IIS site '$siteName'..."
    New-WebSite -Name $siteName `
                -PhysicalPath $physicalPath `
                -ApplicationPool $appPoolName `
                -Port $port `
                -IPAddress "*" `
                -Force | Out-Null
}

Set-ItemProperty "IIS:\Sites\$siteName" -Name applicationPool -Value $appPoolName
Write-Host "Site '$siteName' created on port $port." -ForegroundColor Green

# ── 4. Permissions — grant IIS app pool identity access ─────────────────────
Write-Host "Setting folder permissions for '$appUser'..."
$acl = Get-Acl $physicalPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $appUser, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow'
)
$acl.SetAccessRule($rule)
Set-Acl $physicalPath $acl
Write-Host "Permissions granted." -ForegroundColor Green

# ── 5. Firewall — open port ──────────────────────────────────────────────────
$fwRule = Get-NetFirewallRule -DisplayName "OTA Platform API (HTTP $port)" -ErrorAction SilentlyContinue
if (-not $fwRule) {
    New-NetFirewallRule `
        -DisplayName "OTA Platform API (HTTP $port)" `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort $port `
        -Action Allow `
        -Profile Any | Out-Null
    Write-Host "Firewall rule created for port $port." -ForegroundColor Green
} else {
    Write-Host "Firewall rule for port $port already exists." -ForegroundColor Yellow
}

# ── 6. Start the site ────────────────────────────────────────────────────────
Start-WebSite -Name $siteName -ErrorAction SilentlyContinue
Start-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host "API URL  : http://localhost:$port" -ForegroundColor White
Write-Host "Swagger  : http://localhost:$port/swagger" -ForegroundColor White
Write-Host "Health   : http://localhost:$port/health" -ForegroundColor White
Write-Host "Logs     : $physicalPath\Logs\" -ForegroundColor White
Write-Host ""
Write-Host "To check site status:" -ForegroundColor Yellow
Write-Host "  Get-WebSite -Name '$siteName'" -ForegroundColor Gray
Write-Host "  Get-WebAppPoolState -Name '$appPoolName'" -ForegroundColor Gray
