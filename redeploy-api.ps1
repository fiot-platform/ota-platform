#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Rebuilds and redeploys the OTA Platform API to IIS in one command.
    Run from the OTAPlatform root directory.

    Usage:  .\redeploy-api.ps1
#>

$ErrorActionPreference = 'Stop'
$projectDir   = "$PSScriptRoot\OTA.API"
$publishTemp  = "C:\tmp\ota-api-publish"
$iisPath      = "C:\inetpub\wwwroot\ota-api"
$appPoolName  = 'OTAPlatform-API'
$siteName     = 'OTAPlatform-API'

Import-Module WebAdministration -ErrorAction Stop

Write-Host "`n=== OTA API Redeploy ===" -ForegroundColor Cyan

# 1. Stop IIS site so the exe is not locked
Write-Host "Stopping IIS site..." -ForegroundColor Yellow
Stop-WebSite  -Name $siteName     -ErrorAction SilentlyContinue
Stop-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# 2. Publish
Write-Host "Publishing..." -ForegroundColor Yellow
if (Test-Path $publishTemp) { Remove-Item $publishTemp -Recurse -Force }
& dotnet publish "$projectDir\OTA.API.csproj" `
    -c Release -r win-x64 --self-contained false `
    -o $publishTemp
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# 3. Copy to IIS folder (preserve Logs, firmware-uploads, qa-docs)
Write-Host "Copying files to IIS..." -ForegroundColor Yellow
Get-ChildItem $publishTemp | Where-Object { $_.Name -notin @('Logs','firmware-uploads','qa-docs') } |
    Copy-Item -Destination $iisPath -Recurse -Force

# 4. Overwrite web.config with production version
$webConfig = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath=".\OTA.API.exe" stdoutLogEnabled="true"
                  stdoutLogFile=".\Logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      <httpProtocol>
        <customHeaders>
          <add name="X-Content-Type-Options" value="nosniff" />
          <add name="X-Frame-Options" value="SAMEORIGIN" />
          <remove name="X-Powered-By" /><remove name="Server" />
        </customHeaders>
      </httpProtocol>
      <security>
        <requestFiltering>
          <requestLimits maxAllowedContentLength="104857600" />
        </requestFiltering>
      </security>
    </system.webServer>
  </location>
</configuration>
'@
$webConfig | Set-Content "$iisPath\web.config" -Encoding UTF8

# 5. Restart site
Write-Host "Starting IIS site..." -ForegroundColor Yellow
Start-WebAppPool -Name $appPoolName
Start-Sleep -Seconds 2
Start-WebSite -Name $siteName
Start-Sleep -Seconds 3

# 6. Health check
try {
    $resp = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 10
    Write-Host "Health check: $($resp.Content)" -ForegroundColor Green
} catch {
    Write-Host "Health check failed — check Logs\stdout*.log" -ForegroundColor Red
}

Write-Host "`n=== Redeploy Complete ===" -ForegroundColor Cyan
Write-Host "API     : http://localhost:5000"
Write-Host "Swagger : http://localhost:5000/swagger"
