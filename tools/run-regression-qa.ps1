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
  (Join-Path $desktopRoot "AssemblyInfo.cs"),
  (Join-Path $desktopRoot "AgentDesktop.cs"),
  (Join-Path $desktopRoot "CredentialProtector.cs"),
  (Join-Path $desktopRoot "ModelProviders.cs"),
  (Join-Path $desktopRoot "AgentTools.cs"),
  (Join-Path $desktopRoot "AgentToolExecutors.cs"),
  (Join-Path $desktopRoot "ToolCenterForm.cs"),
  (Join-Path $desktopRoot "VoiceBootstrap.cs")
)
$references = @(
  "System.dll",
  "System.Core.dll",
  "System.Drawing.dll",
  "System.Windows.Forms.dll",
  "System.Net.Http.dll",
  "System.Security.dll",
  "System.IO.Compression.dll",
  "System.IO.Compression.FileSystem.dll",
  "System.Web.Extensions.dll"
)
$packageReferences = @(
  (Join-Path $desktopRoot "packages\PdfPig\lib\net462\UglyToad.PdfPig.dll"),
  (Join-Path $desktopRoot "packages\PdfPig\lib\net462\UglyToad.PdfPig.Core.dll"),
  (Join-Path $desktopRoot "packages\PdfPig\lib\net462\UglyToad.PdfPig.DocumentLayoutAnalysis.dll"),
  (Join-Path $desktopRoot "packages\PdfPig\lib\net462\UglyToad.PdfPig.Fonts.dll"),
  (Join-Path $desktopRoot "packages\PdfPig\lib\net462\UglyToad.PdfPig.Package.dll"),
  (Join-Path $desktopRoot "packages\PdfPig\lib\net462\UglyToad.PdfPig.Tokenization.dll"),
  (Join-Path $desktopRoot "packages\PdfPig\lib\net462\UglyToad.PdfPig.Tokens.dll"),
  (Join-Path $desktopRoot "packages\Microsoft.Bcl.HashCode\lib\net462\Microsoft.Bcl.HashCode.dll"),
  (Join-Path $desktopRoot "packages\System.Buffers\lib\net462\System.Buffers.dll"),
  (Join-Path $desktopRoot "packages\System.Memory\lib\net462\System.Memory.dll"),
  (Join-Path $desktopRoot "packages\System.Numerics.Vectors\lib\net462\System.Numerics.Vectors.dll"),
  (Join-Path $desktopRoot "packages\System.Runtime.CompilerServices.Unsafe\lib\net462\System.Runtime.CompilerServices.Unsafe.dll")
)
foreach ($packageReference in $packageReferences) {
  if (-not (Test-Path -LiteralPath $packageReference)) { throw "Required PDF dependency is missing: $packageReference" }
}
$references += $packageReferences
foreach ($packageReference in $packageReferences) {
  Copy-Item -LiteralPath $packageReference -Destination $output -Force
}

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

$modelProviderExe = Build-Harness `
  "ModelProviderQa" `
  "IrohaAgentDesktop.ModelProviderQaProgram" `
  (Join-Path $PSScriptRoot "ModelProviderQaHarness.cs")
Run-Harness $modelProviderExe @(
  "--output", (Join-Path $output "model-providers.txt")
)

$agentToolsExe = Build-Harness `
  "AgentToolsQa" `
  "IrohaAgentDesktop.AgentToolsQaProgram" `
  (Join-Path $PSScriptRoot "AgentToolsQaHarness.cs")
Run-Harness $agentToolsExe @(
  "--output", (Join-Path $output "agent-tools.txt"),
  "--work-root", (Join-Path $output "agent-tools-work")
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

$settingsUiExe = Build-Harness `
  "SettingsUiQa" `
  "IrohaAgentDesktop.SettingsUiQaProgram" `
  (Join-Path $PSScriptRoot "SettingsUiQaHarness.cs")
Run-Harness $settingsUiExe @(
  "--model-screenshot", (Join-Path $output "settings-model.png"),
  "--voice-screenshot", (Join-Path $output "settings-voice.png"),
  "--compact-screenshot", (Join-Path $output "settings-compact-main.png"),
  "--tool-screenshot", (Join-Path $output "settings-tools.png"),
  "--tool-privacy-screenshot", (Join-Path $output "settings-tools-privacy.png"),
  "--tool-skills-screenshot", (Join-Path $output "settings-tools-skills.png"),
  "--report", (Join-Path $output "settings-ui.txt"),
  "--width", "1672",
  "--height", "941"
)

Write-Host "Regression QA passed: $output"
Get-ChildItem -LiteralPath $output -Filter "*.txt" -File |
  Sort-Object Name |
  Select-Object Name, Length
