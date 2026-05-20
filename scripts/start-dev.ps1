# Prompt for admin credentials, set them via user-secrets, then start backend and frontend.
param()

Write-Host "This script will prompt for admin credentials and start the backend and frontend dev servers."

$adminUser = Read-Host "Admin username"
$adminPass = Read-Host -AsSecureString "Admin password"
$plainPass = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminPass))

# Initialize user-secrets if missing
Push-Location ..\
if (-not (Test-Path -Path .\uis-bachelor-sustainability-webapp.csproj)) { Pop-Location; Push-Location (Get-Location) }

try {
    dotnet user-secrets init | Out-Null
} catch {}

# Set secrets for project
dotnet user-secrets set "ADMIN_USER" $adminUser | Out-Null
dotnet user-secrets set "ADMIN_PASSWORD" $plainPass | Out-Null

# Start backend and frontend
Write-Host "Starting backend (dotnet watch run)..."
Start-Process -NoNewWindow -FilePath pwsh -ArgumentList '-NoExit','-Command','dotnet watch run' -WorkingDirectory (Get-Location)

# Start frontend in Transparent
Write-Host "Starting frontend (npm run dev) in Transparent..."
Start-Process -NoNewWindow -FilePath pwsh -ArgumentList '-NoExit','-Command','cd Transparent; npm install; npm run dev' -WorkingDirectory (Get-Location)

Write-Host "Dev servers launched. Close this window to stop prompts." 
