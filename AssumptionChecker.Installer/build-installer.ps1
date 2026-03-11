# <summary>
# Builds the AssumptionChecker MSI installer.
# 1. Publishes WPFApp and Engine as self-contained win-x64
# 2. Generates WiX component WXS files from the publish output (no Heat required)
# 3. Builds the WiX v4 installer project into an MSI
#
# Prerequisites:
#   - .NET 8 SDK  (https://dotnet.microsoft.com/download/dotnet/8.0)
#
#   The global 'wix' CLI tool is NOT required — the WiX SDK resolves its
#   toolchain entirely through NuGet. WXS harvesting is done by this script.
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

# == recursive: walks a directory and appends Component/Directory WiX XML == #
function Add-WixDirTree {
    param(
        [System.IO.DirectoryInfo]$Dir,
        [string]$BaseDir,
        [string]$IdPrefix,
        [System.Text.StringBuilder]$Xml,
        [System.Collections.Generic.List[string]]$Refs,
        [int]$Depth = 0
    )
    $pad = '  ' * ($Depth + 3)

    # == one Component per file == #
    foreach ($f in $Dir.GetFiles()) {
        $rel   = $f.FullName.Substring($BaseDir.Length + 1)
        $safe  = "${IdPrefix}_" + ($rel  -replace '[^A-Za-z0-9]', '_')
        $cmpId = "cmp_$safe"
        $filId = "fil_$safe"
        $null  = $Xml.AppendLine("$pad<Component Id=`"$cmpId`" Guid=`"*`">")
        $null  = $Xml.AppendLine("$pad  <File Id=`"$filId`" Source=`"$($f.FullName)`" KeyPath=`"yes`" />")
        $null  = $Xml.AppendLine("$pad</Component>")
        $Refs.Add("      <ComponentRef Id=`"$cmpId`" />")
    }

    # == recurse into subdirectories — IdPrefix prevents cross-group ID clashes == #
    foreach ($sub in $Dir.GetDirectories()) {
        $relDir = $sub.FullName.Substring($BaseDir.Length + 1)
        $dirId  = "d_${IdPrefix}_" + ($relDir -replace '[^A-Za-z0-9]', '_')
        $null   = $Xml.AppendLine("$pad<Directory Id=`"$dirId`" Name=`"$($sub.Name)`">")
        Add-WixDirTree -Dir $sub -BaseDir $BaseDir -IdPrefix $IdPrefix `
                       -Xml $Xml -Refs $Refs -Depth ($Depth + 1)
        $null   = $Xml.AppendLine("$pad</Directory>")
    }
}

# == generate a WiX v4 fragment WXS from a publish output directory == #
function Write-WixComponents {
    param(
        [string]$SourceDir,
        [string]$ComponentGroupName,
        [string]$RootDirRefId,
        [string]$IdPrefix,
        [string]$OutFile
    )
    $absSource = (Resolve-Path $SourceDir).Path.TrimEnd('\')
    $xml  = [System.Text.StringBuilder]::new()
    $refs = [System.Collections.Generic.List[string]]::new()

    Add-WixDirTree -Dir (Get-Item $absSource) -BaseDir $absSource `
                   -IdPrefix $IdPrefix -Xml $xml -Refs $refs

    $content = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <DirectoryRef Id="$RootDirRefId">
$($xml.ToString().TrimEnd())
    </DirectoryRef>
    <ComponentGroup Id="$ComponentGroupName">
$($refs -join "`n")
    </ComponentGroup>
  </Fragment>
</Wix>
"@
    Set-Content -Path $OutFile -Value $content -Encoding UTF8
    Write-Host "  -> $OutFile ($($refs.Count) components)"
}

try {
    # == Step 1: Clean previous publish output == #
    Write-Host "`n=== Cleaning publish output ===" -ForegroundColor Cyan
    if (Test-Path "publish") { Remove-Item -Recurse -Force "publish" }

    # == Step 2: Publish WPFApp (self-contained, win-x64) == #
    # IsInstallerBuild=true suppresses the CopyEngine AfterBuild target
    Write-Host "`n=== Publishing WPFApp ===" -ForegroundColor Cyan
    dotnet publish AssumptionChecker.WPFApp\AssumptionChecker.WPFApp.csproj `
        -c $Configuration `
        -p:PublishProfile=Installer `
        -p:IsInstallerBuild=true

    if ($LASTEXITCODE -ne 0) { throw "WPFApp publish failed" }

    # == Step 3: Publish Engine (self-contained, win-x64) == #
    Write-Host "`n=== Publishing Engine ===" -ForegroundColor Cyan
    dotnet publish AssumptionChecker.Engine\AssumptionChecker.Engine.csproj `
        -c $Configuration `
        -p:PublishProfile=Installer `
        -p:IsInstallerBuild=true

    if ($LASTEXITCODE -ne 0) { throw "Engine publish failed" }

    # == Step 4: Generate WiX component WXS files from publish output == #
    # IdPrefix isolates component/directory IDs between the two groups so
    # shared subdirectory names (e.g. runtimes\win-x64) don't collide.
    Write-Host "`n=== Generating WiX component files ===" -ForegroundColor Cyan
    Write-WixComponents `
        -SourceDir          "publish\wpfapp" `
        -ComponentGroupName "WPFAppFiles" `
        -RootDirRefId       "INSTALLFOLDER" `
        -IdPrefix           "wpf" `
        -OutFile            "AssumptionChecker.Installer\WPFAppFiles.wxs"

    Write-WixComponents `
        -SourceDir          "publish\engine" `
        -ComponentGroupName "EngineFiles" `
        -RootDirRefId       "EngineFolder" `
        -IdPrefix           "eng" `
        -OutFile            "AssumptionChecker.Installer\EngineFiles.wxs"

    # == Step 5: Build the MSI == #
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
