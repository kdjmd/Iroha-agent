[CmdletBinding()]
param(
  [string]$OutputRoot
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
  $OutputRoot = Join-Path $PSScriptRoot "..\tmp\regression-qa"
}
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$desktopRoot = Join-Path $projectRoot "desktop"
$output = [IO.Path]::GetFullPath($OutputRoot)
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $csc)) {
  $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path -LiteralPath $csc)) { throw "Cannot find .NET Framework csc.exe" }

if (Test-Path -LiteralPath $output) {
  $allowedRoots = @(
    [IO.Path]::GetFullPath((Join-Path $projectRoot "tmp")),
    [IO.Path]::GetFullPath($env:TEMP)
  )
  $safeToClean = $false
  foreach ($allowedRoot in $allowedRoots) {
    $prefix = $allowedRoot.TrimEnd('\') + '\'
    if ($output.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
      $safeToClean = $true
      break
    }
  }
  if (-not $safeToClean) { throw "Refusing to clean QA output outside project/system temp: $output" }
  Remove-Item -LiteralPath $output -Recurse -Force
}

New-Item -ItemType Directory -Path $output -Force | Out-Null

$commonSources = @(
  (Join-Path $desktopRoot "AgentDesktop.cs"),
  (Join-Path $desktopRoot "VoiceBootstrap.cs")
)
$references = @(
  "System.dll",
  "System.Core.dll",
  "System.Drawing.dll",
  "System.Windows.Forms.dll",
  "System.Net.Http.dll",
  "System.IO.Compression.dll",
  "System.IO.Compression.FileSystem.dll",
  "System.Web.Extensions.dll"
)

function Build-Harness([string]$Name, [string]$MainType, [string]$HarnessSource) {
  $exe = Join-Path $output ($Name + ".exe")
  $arguments = @(
    "/nologo",
    "/codepage:65001",
    "/utf8output",
    "/target:exe",
    "/main:$MainType",
    "/out:$exe"
  )
  $arguments += $references | ForEach-Object { "/r:$_" }
  $arguments += $commonSources
  $arguments += $HarnessSource
  & $csc @arguments
  if ($LASTEXITCODE -ne 0) { throw "$Name compilation failed with exit code $LASTEXITCODE" }
  return $exe
}

function Run-Harness([string]$Exe, [string[]]$Arguments) {
  & $Exe @Arguments
  if ($LASTEXITCODE -ne 0) {
    throw "$(Split-Path $Exe -Leaf) failed with exit code $LASTEXITCODE"
  }
}

& (Join-Path $desktopRoot "build.ps1")

$memoryExe = Build-Harness `
  "MemoryStoreQa" `
  "IrohaAgentDesktop.MemoryStoreQaProgram" `
  (Join-Path $PSScriptRoot "MemoryStoreQaHarness.cs")
Run-Harness $memoryExe @(
  "--output", (Join-Path $output "memory-store.txt"),
  "--work-root", (Join-Path $output "memory-work")
)

$voiceExe = Build-Harness `
  "VoiceBootstrapQa" `
  "IrohaAgentDesktop.VoiceBootstrapQaProgram" `
  (Join-Path $PSScriptRoot "VoiceBootstrapQaHarness.cs")
Run-Harness $voiceExe @(
  "--output", (Join-Path $output "voice-bootstrap.txt"),
  "--work-root", (Join-Path $output "voice-work")
)

$env:IROHA_APP_DATA_ROOT = Join-Path $output "functional-app-data"
$env:IROHA_VOICE_MANAGED_ROOT = Join-Path $output "functional-voice-data"
$functionalExe = Build-Harness `
  "FunctionalQa" `
  "IrohaAgentDesktop.FunctionalQaProgram" `
  (Join-Path $PSScriptRoot "FunctionalQaHarness.cs")
Copy-Item -LiteralPath (Join-Path $projectRoot "assets") -Destination $output -Recurse -Force
Run-Harness $functionalExe @("--output", (Join-Path $output "functional.txt"))

Write-Host "Regression QA passed: $output"
Get-ChildItem -LiteralPath $output -Filter "*.txt" -File |
  Sort-Object Name |
  Select-Object Name, Length
