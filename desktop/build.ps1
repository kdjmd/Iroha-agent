$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root "dist"
$assemblyInfoSource = Join-Path $root "AssemblyInfo.cs"
$source = Join-Path $root "AgentDesktop.cs"
$credentialProtectorSource = Join-Path $root "CredentialProtector.cs"
$modelProvidersSource = Join-Path $root "ModelProviders.cs"
$agentToolsSource = Join-Path $root "AgentTools.cs"
$agentToolExecutorsSource = Join-Path $root "AgentToolExecutors.cs"
$toolCenterSource = Join-Path $root "ToolCenterForm.cs"
$voiceBootstrapSource = Join-Path $root "VoiceBootstrap.cs"
$exe = Join-Path $dist "IrohaAgent.exe"
$projectRoot = Split-Path -Parent $root
$icon = Join-Path $projectRoot "assets\icons\caiye-chibi.ico"

if (-not (Test-Path -LiteralPath $dist)) {
  New-Item -ItemType Directory -Path $dist | Out-Null
}

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $csc)) {
  $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path -LiteralPath $csc)) {
  throw "Cannot find .NET Framework csc.exe"
}

$cscArgs = @(
  "/nologo",
  "/codepage:65001",
  "/utf8output",
  "/target:winexe",
  "/out:$exe",
  "/r:System.dll",
  "/r:System.Core.dll",
  "/r:System.Drawing.dll",
  "/r:System.Windows.Forms.dll",
  "/r:System.Net.Http.dll",
  "/r:System.Security.dll",
  "/r:System.IO.Compression.dll",
  "/r:System.IO.Compression.FileSystem.dll",
  "/r:System.Web.Extensions.dll"
)

$packageDlls = @(
  (Join-Path $root "packages\PdfPig\lib\net462\UglyToad.PdfPig.dll"),
  (Join-Path $root "packages\PdfPig\lib\net462\UglyToad.PdfPig.Core.dll"),
  (Join-Path $root "packages\PdfPig\lib\net462\UglyToad.PdfPig.DocumentLayoutAnalysis.dll"),
  (Join-Path $root "packages\PdfPig\lib\net462\UglyToad.PdfPig.Fonts.dll"),
  (Join-Path $root "packages\PdfPig\lib\net462\UglyToad.PdfPig.Package.dll"),
  (Join-Path $root "packages\PdfPig\lib\net462\UglyToad.PdfPig.Tokenization.dll"),
  (Join-Path $root "packages\PdfPig\lib\net462\UglyToad.PdfPig.Tokens.dll"),
  (Join-Path $root "packages\Microsoft.Bcl.HashCode\lib\net462\Microsoft.Bcl.HashCode.dll"),
  (Join-Path $root "packages\System.Buffers\lib\net462\System.Buffers.dll"),
  (Join-Path $root "packages\System.Memory\lib\net462\System.Memory.dll"),
  (Join-Path $root "packages\System.Numerics.Vectors\lib\net462\System.Numerics.Vectors.dll"),
  (Join-Path $root "packages\System.Runtime.CompilerServices.Unsafe\lib\net462\System.Runtime.CompilerServices.Unsafe.dll")
)
foreach ($packageDll in $packageDlls) {
  if (-not (Test-Path -LiteralPath $packageDll)) { throw "Required PDF dependency is missing: $packageDll" }
  $cscArgs += "/r:$packageDll"
}

if (Test-Path -LiteralPath $icon) {
  $cscArgs += "/win32icon:$icon"
}

$cscArgs += $assemblyInfoSource
$cscArgs += $source
$cscArgs += $credentialProtectorSource
$cscArgs += $modelProvidersSource
$cscArgs += $agentToolsSource
$cscArgs += $agentToolExecutorsSource
$cscArgs += $toolCenterSource
$cscArgs += $voiceBootstrapSource
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) {
  throw "C# compiler failed with exit code $LASTEXITCODE"
}

foreach ($packageDll in $packageDlls) {
  Copy-Item -LiteralPath $packageDll -Destination $dist -Force
}

$characterSource = Join-Path $projectRoot "assets\character"
$assetSource = Join-Path $characterSource "frames"
$assetTarget = Join-Path $dist "assets"
$characterTarget = Join-Path $assetTarget "character"
$frameTarget = Join-Path $assetTarget "character\frames"
$expressionSource = Join-Path $characterSource "expressions"
$expressionTarget = Join-Path $assetTarget "character\expressions"
$uiSource = Join-Path $projectRoot "assets\ui"
$uiTarget = Join-Path $assetTarget "ui"
if (Test-Path -LiteralPath $assetSource) {
  $resolvedTarget = [System.IO.Path]::GetFullPath($frameTarget)
  $resolvedDist = [System.IO.Path]::GetFullPath($dist)
  if (-not $resolvedTarget.StartsWith($resolvedDist, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Asset target escapes dist: $resolvedTarget"
  }
  if (Test-Path -LiteralPath $assetTarget) {
    Remove-Item -LiteralPath $assetTarget -Recurse -Force
  }
  New-Item -ItemType Directory -Force -Path $frameTarget | Out-Null
  Get-ChildItem -LiteralPath $assetSource -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $frameTarget -Force
  }
}

if (Test-Path -LiteralPath $characterSource) {
  $resolvedTarget = [System.IO.Path]::GetFullPath($characterTarget)
  $resolvedDist = [System.IO.Path]::GetFullPath($dist)
  if (-not $resolvedTarget.StartsWith($resolvedDist, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Character asset target escapes dist: $resolvedTarget"
  }
  New-Item -ItemType Directory -Force -Path $characterTarget | Out-Null
  Get-ChildItem -LiteralPath $characterSource -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $characterTarget -Force
  }
}

if (Test-Path -LiteralPath $expressionSource) {
  $resolvedTarget = [System.IO.Path]::GetFullPath($expressionTarget)
  $resolvedDist = [System.IO.Path]::GetFullPath($dist)
  if (-not $resolvedTarget.StartsWith($resolvedDist, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Expression asset target escapes dist: $resolvedTarget"
  }
  New-Item -ItemType Directory -Force -Path $expressionTarget | Out-Null
  Get-ChildItem -LiteralPath $expressionSource -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $expressionTarget -Force
  }
}

if (Test-Path -LiteralPath $uiSource) {
  $resolvedTarget = [System.IO.Path]::GetFullPath($uiTarget)
  $resolvedDist = [System.IO.Path]::GetFullPath($dist)
  if (-not $resolvedTarget.StartsWith($resolvedDist, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "UI asset target escapes dist: $resolvedTarget"
  }
  New-Item -ItemType Directory -Force -Path $uiTarget | Out-Null
  Get-ChildItem -LiteralPath $uiSource -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $uiTarget -Force
  }
}

$launcher = Join-Path $root "Start-IrohaAgent.bat"
if (Test-Path -LiteralPath $launcher) {
  Copy-Item -LiteralPath $launcher -Destination (Join-Path $dist "Start-IrohaAgent.bat") -Force
}

Write-Host "Built $exe"
