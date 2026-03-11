# <summary>
# Builds the AssumptionChecker MSI installer.
# 1. Publishes WPFApp and Engine as self-contained win-x64
# 2. Builds the WiX v4 installer project into an MSI
#
# Prerequisites:
#   - .NET 8 SDK
#   - WiX Toolset v4 .NET tool:  dotnet tool install --global wix
#   - WiX Heat extension:        wix extension add WixToolset.Heat/4.0.5
#
# Output: AssumptionChecker.Installer\bin\Release\AssumptionChecker.Installer.msi
# </summary>

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $root) { $root = $PSScriptRoot }
Push-Location $root

try {
    # == Step 1: Clean previous publish output == #
    Write-Host "`n=== Cleaning publish output ===" -ForegroundColor Cyan
    if (Test-Path "publish") { Remove-Item -Recurse -Force "publish" }

    # == Step 2: Publish WPFApp (self-contained, win-x64) == #
    Write-Host "`n=== Publishing WPFApp ===" -ForegroundColor Cyan
    dotnet publish AssumptionChecker.WPFApp\AssumptionChecker.WPFApp.csproj `
        -c $Configuration `
        -p:PublishProfile=Installer `
        --no-restore

    if ($LASTEXITCODE -ne 0) { throw "WPFApp publish failed" }

    # == Step 3: Publish Engine (self-contained, win-x64) == #
    Write-Host "`n=== Publishing Engine ===" -ForegroundColor Cyan
    dotnet publish AssumptionChecker.Engine\AssumptionChecker.Engine.csproj `
        -c $Configuration `
        -p:PublishProfile=Installer `
        --no-restore

    if ($LASTEXITCODE -ne 0) { throw "Engine publish failed" }

    # == Step 4: Build the MSI == #
    Write-Host "`n=== Building MSI installer ===" -ForegroundColor Cyan
    dotnet build AssumptionChecker.Installer\AssumptionChecker.Installer.wixproj `
        -c $Configuration

    if ($LASTEXITCODE -ne 0) { throw "MSI build failed" }

    # == Done == #
    $msiPath = Get-ChildItem "AssumptionChecker.Installer\bin\$Configuration\*.msi" |
               Select-Object -First 1 -ExpandProperty FullName

    Write-Host "`n=== BUILD SUCCEEDED ===" -ForegroundColor Green
    Write-Host "MSI: $msiPath" -ForegroundColor Green
}
finally {
    Pop-Location
}
