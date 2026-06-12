# Configure admin credentials via user-secrets, then launch backend and frontend dev servers.
param(
    [string]$AdminUser,
    [string]$AdminPassword,
    [switch]$SkipFrontendInstall
)

$ErrorActionPreference = 'Stop'

function Get-PreferredShell {
    $pwshCommand = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($null -ne $pwshCommand) {
        return $pwshCommand.Source
    }

    $powershellCommand = Get-Command powershell -ErrorAction SilentlyContinue
    if ($null -ne $powershellCommand) {
        return $powershellCommand.Source
    }

    throw 'Could not find pwsh or powershell in PATH.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $repoRoot 'uis-bachelor-sustainability-webapp.csproj'
$frontendPath = Join-Path $repoRoot 'Transparent'
$frontendPackageJson = Join-Path $frontendPath 'package.json'

if (-not (Test-Path -Path $projectFile)) {
    throw "Could not find project file: $projectFile"
}

if (-not (Test-Path -Path $frontendPackageJson)) {
    throw "Could not find frontend package.json: $frontendPackageJson"
}

Write-Host 'Preparing local development environment...'

if ([string]::IsNullOrWhiteSpace($AdminUser)) {
    $AdminUser = Read-Host 'Admin username'
}

if ([string]::IsNullOrWhiteSpace($AdminPassword)) {
    $secureAdminPassword = Read-Host -AsSecureString 'Admin password (minimum 6 characters)'
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureAdminPassword)
    try {
        $AdminPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

if ([string]::IsNullOrWhiteSpace($AdminUser) -or [string]::IsNullOrWhiteSpace($AdminPassword)) {
    throw 'Admin username and password are required.'
}

if ($AdminPassword.Length -lt 6) {
    throw 'Admin password must be at least 6 characters.'
}

Write-Host 'Configuring user-secrets for admin bootstrap credentials...'
dotnet user-secrets init --project $projectFile | Out-Null
dotnet user-secrets set "ADMIN_BOOTSTRAP_USER" $AdminUser --project $projectFile | Out-Null
dotnet user-secrets set "ADMIN_BOOTSTRAP_PASSWORD" $AdminPassword --project $projectFile | Out-Null
dotnet user-secrets set "ADMIN_USER" $AdminUser --project $projectFile | Out-Null
dotnet user-secrets set "ADMIN_PASSWORD" $AdminPassword --project $projectFile | Out-Null

$shellExe = Get-PreferredShell

Write-Host 'Starting backend (dotnet watch run)...'
Start-Process -NoNewWindow -FilePath $shellExe -ArgumentList '-NoExit','-Command','dotnet watch run' -WorkingDirectory $repoRoot

$frontendCommand = if ($SkipFrontendInstall) { 'npm run dev' } else { 'npm ci; npm run dev' }

Write-Host 'Starting frontend (npm run dev) in Transparent...'
Start-Process -NoNewWindow -FilePath $shellExe -ArgumentList '-NoExit','-Command',$frontendCommand -WorkingDirectory $frontendPath

Write-Host 'Done. Backend and frontend dev servers were launched in separate shell windows.'
Write-Host 'Open the frontend URL from the npm terminal (usually http://localhost:5173) and go to /admin/login.'
