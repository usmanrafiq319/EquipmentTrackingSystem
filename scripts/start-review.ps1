[CmdletBinding()]
param([switch]$NoLaunch)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
function Require-Command($name, $versionArgs) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) { throw "Required prerequisite '$name' was not found." }
  $v = & $name @versionArgs 2>&1 | Select-Object -First 1
  Write-Host "$name $v"
}
function Invoke-Checked([string]$file, [string[]]$arguments) {
  & $file @arguments
  if ($LASTEXITCODE -ne 0) { throw "$file failed with exit code $LASTEXITCODE." }
}
Require-Command dotnet @('--version')
Require-Command node @('--version')
Require-Command npm @('--version')
$installedSdks = (& dotnet --list-sdks | Out-String)
if ($installedSdks -notmatch '(?m)^8\.') { throw 'The .NET 8 SDK is required.' }
if ((& node --version) -notmatch '^v24\.') { throw 'Node.js 24.x is required for the pinned Angular 21 toolchain.' }
$api = Join-Path $root 'src\EquipmentTracking.Api'
$web = Join-Path $root 'src\equipment-tracking-web'
$db = Join-Path $api 'data\equipment-tracking.db'
New-Item -ItemType Directory -Force (Split-Path $db) | Out-Null
Invoke-Checked dotnet @('restore', "$api\EquipmentTracking.Api.csproj", '-p:TargetFramework=net8.0')
Invoke-Checked dotnet @('tool', 'restore', '--tool-manifest', "$root\dotnet-tools.json")
Invoke-Checked dotnet @('build', "$api\EquipmentTracking.Api.csproj", '--framework', 'net8.0', '--no-restore')
try {
  Push-Location $web
  Invoke-Checked npm @('ci')
  Invoke-Checked npm @('run', 'build')
}
finally {
  Pop-Location
}
$dist = Join-Path $web 'dist\equipment-tracking-web\browser'
$wwwroot = Join-Path $api 'wwwroot'
if (-not (Test-Path (Join-Path $dist 'index.html'))) { throw 'Angular production output was not generated.' }
Remove-Item $wwwroot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $wwwroot | Out-Null
Copy-Item (Join-Path $dist '*') $wwwroot -Recurse -Force
Invoke-Checked dotnet @('ef', 'database', 'update', '--project', "$api\EquipmentTracking.Api.csproj", '--framework', 'net8.0')
Invoke-Checked dotnet @('test', "$root\tests\EquipmentTracking.IntegrationTests\EquipmentTracking.IntegrationTests.csproj", '--framework', 'net8.0')
if ($NoLaunch) { Write-Host "Review environment verified (NoLaunch)." ; exit 0 }
Invoke-Checked dotnet @('run', '--project', "$api\EquipmentTracking.Api.csproj", '--framework', 'net8.0', '--no-build', '--urls', 'http://localhost:5080')
