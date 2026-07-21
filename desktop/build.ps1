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
$assetTarget = Join-Path $dist "assets"
$characterTarget = Join-Path $assetTarget "character"
$expressionSource = Join-Path $characterSource "expressions"
$expressionTarget = Join-Path $assetTarget "character\expressions"
$uiSource = Join-Path $projectRoot "assets\ui"
$uiTarget = Join-Path $assetTarget "ui"
$resolvedAssetTarget = [System.IO.Path]::GetFullPath($assetTarget)
$resolvedDist = [System.IO.Path]::GetFullPath($dist)
if (-not $resolvedAssetTarget.StartsWith($resolvedDist, [System.StringComparison]::OrdinalIgnoreCase)) {
  throw "Asset target escapes dist: $resolvedAssetTarget"
}
if (Test-Path -LiteralPath $assetTarget) {
  Remove-Item -LiteralPath $assetTarget -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $characterTarget | Out-Null
New-Item -ItemType Directory -Force -Path $expressionTarget | Out-Null
New-Item -ItemType Directory -Force -Path $uiTarget | Out-Null

function Copy-RequiredAsset([string]$sourcePath, [string]$targetPath) {
  if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Required runtime asset is missing: $sourcePath"
  }
  Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
}

$characterFiles = @("iroha-portrait.png")
$expressionFiles = @(
  "iroha-blink-closed.png",
  "iroha-blink-half.png",
  "iroha-speak-open.png",
  "iroha-speak-small.png"
)
$uiFiles = @(
  "vn-room-bg.png",
  "iroha-chibi-card-v2.png",
  "official-iroha-real.png",
  "official-character-01.png",
  "official-character-02.png",
  "official-character-03.png",
  "official-character-04.png",
  "official-character-05.png",
  "official-character-06.png",
  "official-character-07.png",
  "official-character-08.png",
  "official-character-09.png",
  "official-character-10.png",
  "official-character-11.png",
  "official-character-12.png"
)

foreach ($file in $characterFiles) {
  Copy-RequiredAsset (Join-Path $characterSource $file) (Join-Path $characterTarget $file)
}
foreach ($file in $expressionFiles) {
  Copy-RequiredAsset (Join-Path $expressionSource $file) (Join-Path $expressionTarget $file)
}
foreach ($file in $uiFiles) {
  Copy-RequiredAsset (Join-Path $uiSource $file) (Join-Path $uiTarget $file)
}

$expectedAssetCount = $characterFiles.Count + $expressionFiles.Count + $uiFiles.Count
$actualAssetCount = @(Get-ChildItem -LiteralPath $assetTarget -Recurse -File).Count
if ($actualAssetCount -ne $expectedAssetCount) {
  throw "Runtime asset allowlist mismatch: expected $expectedAssetCount files, found $actualAssetCount"
}

$launcher = Join-Path $root "Start-IrohaAgent.bat"
if (Test-Path -LiteralPath $launcher) {
  Copy-Item -LiteralPath $launcher -Destination (Join-Path $dist "Start-IrohaAgent.bat") -Force
}

Write-Host "Built $exe"
