param(
  [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$desktop = [Environment]::GetFolderPath("DesktopDirectory")
$dist = Join-Path $projectRoot "desktop\dist"
$desktopApp = Join-Path $desktop "IrohaAgent-Latest"
$desktopZip = Join-Path $desktop "IrohaAgent-Latest.zip"
$sourceStage = Join-Path $env:TEMP "IrohaAgent-Source-Latest"
$sourceZip = Join-Path $desktop "IrohaAgent-Source-Latest.zip"

if (-not $SkipBuild) {
  & powershell -ExecutionPolicy Bypass -File (Join-Path $projectRoot "desktop\build.ps1")
}

$resolvedDesktop = [System.IO.Path]::GetFullPath($desktop)
$resolvedApp = [System.IO.Path]::GetFullPath($desktopApp)
if (-not $resolvedApp.StartsWith($resolvedDesktop, [System.StringComparison]::OrdinalIgnoreCase)) {
  throw "Desktop app target escapes desktop: $resolvedApp"
}

if (Test-Path -LiteralPath $desktopApp) {
  Remove-Item -LiteralPath $desktopApp -Recurse -Force
}
New-Item -ItemType Directory -Path $desktopApp | Out-Null
Copy-Item -LiteralPath (Join-Path $dist "IrohaAgent.exe") -Destination $desktopApp -Force
Copy-Item -LiteralPath (Join-Path $dist "Start-IrohaAgent.bat") -Destination $desktopApp -Force
Copy-Item -LiteralPath (Join-Path $dist "assets") -Destination $desktopApp -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "README.md") -Destination $desktopApp -Force
if (Test-Path -LiteralPath (Join-Path $projectRoot "docs\OPTIMIZATION_LOG.md")) {
  Copy-Item -LiteralPath (Join-Path $projectRoot "docs\OPTIMIZATION_LOG.md") -Destination $desktopApp -Force
}

$desktopDocs = Join-Path $desktopApp "docs"
New-Item -ItemType Directory -Path $desktopDocs -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $projectRoot "docs") -File |
  Where-Object { $_.Extension -eq ".docx" -or ($_.Extension -eq ".md" -and $_.Name -ne "OPTIMIZATION_LOG.md") } |
  ForEach-Object {
  Copy-Item -LiteralPath $_.FullName -Destination $desktopDocs -Force
}
if (Test-Path -LiteralPath (Join-Path $projectRoot "docs\evidence")) {
  $desktopEvidence = Join-Path $desktopDocs "evidence"
  New-Item -ItemType Directory -Path $desktopEvidence -Force | Out-Null
  $releaseEvidence = @(
    "round-2026-07-15-v18-refined-standard.png",
    "round-2026-07-15-v18-final-comparison-full.png",
    "round-2026-07-15-v18-final-comparison-left.png",
    "round-2026-07-15-v18-final-comparison-bottom.png",
    "round-2026-07-15-v18-final-comparison-stage.png",
    "round-2026-07-15-v18-natural-blink-contact.png",
    "round-2026-07-15-v18-natural-speech-contact.png",
    "round-2026-07-15-v18-refined-compact.png",
    "round-2026-07-15-v18-refined-settings.png",
    "round-2026-07-15-v18-functional-qa.txt",
    "round-2026-07-15-v18-packaged-final.png",
    "round-2026-07-16-v19-auto-voice-ready.png",
    "round-2026-07-16-v19-voice-speaking.png",
    "round-2026-07-16-v19-voice-comparison-full.png",
    "round-2026-07-16-v19-voice-comparison-bottom.png",
    "round-2026-07-16-v19-voice-qa.txt",
    "round-2026-07-16-v19-voice-ui-qa.txt",
    "round-2026-07-16-v19-functional-qa.txt",
    "round-2026-07-16-v19-packaged-final.png",
    "round-2026-07-16-v20-rail-service-standard.png",
    "round-2026-07-16-v20-rail-service-compact.png",
    "round-2026-07-16-v20-rail-settings.png",
    "round-2026-07-16-v20-comparison-full.png",
    "round-2026-07-16-v20-rail-service-comparison.png",
    "round-2026-07-16-v20-functional-qa.txt",
    "round-2026-07-16-v20-voice-qa.txt",
    "round-2026-07-16-v20-voice-ui-qa.txt",
    "round-2026-07-16-v20-voice-speaking.png",
    "round-2026-07-16-v20-packaged-final.png",
    "round-2026-07-16-v20-packaged-voice-speaking.png"
  )
  foreach ($file in $releaseEvidence) {
    $candidate = Join-Path $projectRoot ("docs\evidence\" + $file)
    if (Test-Path -LiteralPath $candidate) {
      Copy-Item -LiteralPath $candidate -Destination $desktopEvidence -Force
    }
  }
}
if (Test-Path -LiteralPath (Join-Path $projectRoot "design-qa.md")) {
  Copy-Item -LiteralPath (Join-Path $projectRoot "design-qa.md") -Destination $desktopDocs -Force
}

if (Test-Path -LiteralPath $desktopZip) {
  Remove-Item -LiteralPath $desktopZip -Force
}
Compress-Archive -Path (Join-Path $desktopApp "*") -DestinationPath $desktopZip -Force

$resolvedTemp = [System.IO.Path]::GetFullPath($env:TEMP)
$resolvedStage = [System.IO.Path]::GetFullPath($sourceStage)
if (-not $resolvedStage.StartsWith($resolvedTemp, [System.StringComparison]::OrdinalIgnoreCase)) {
  throw "Source stage target escapes temp: $resolvedStage"
}

if (Test-Path -LiteralPath $sourceStage) {
  Remove-Item -LiteralPath $sourceStage -Recurse -Force
}
New-Item -ItemType Directory -Path $sourceStage | Out-Null

$excludeDirs = @(
  ".git", ".toolchain", ".gradle-user", "package", "tmp",
  "desktop\dist", "android\.gradle", "android\app\build", "android\build"
)

Get-ChildItem -LiteralPath $projectRoot -Force | ForEach-Object {
  $relative = $_.Name
  if ($excludeDirs -contains $relative) { return }
  if ($_.PSIsContainer) {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $sourceStage $_.Name) -Recurse -Force
  } else {
    if ($_.Extension -notin @(".zip", ".apk", ".log")) {
      Copy-Item -LiteralPath $_.FullName -Destination $sourceStage -Force
    }
  }
}

foreach ($dir in $excludeDirs) {
  $candidate = Join-Path $sourceStage $dir
  if (Test-Path -LiteralPath $candidate) {
    Remove-Item -LiteralPath $candidate -Recurse -Force
  }
}

Get-ChildItem -LiteralPath $sourceStage -Directory -Recurse -Force |
  Where-Object { $_.Name -in @("__pycache__", ".pytest_cache") } |
  Sort-Object { $_.FullName.Length } -Descending |
  Remove-Item -Recurse -Force
Get-ChildItem -LiteralPath $sourceStage -File -Recurse -Force -Filter "*.pyc" |
  Remove-Item -Force

if (Test-Path -LiteralPath $sourceZip) {
  Remove-Item -LiteralPath $sourceZip -Force
}
Compress-Archive -Path (Join-Path $sourceStage "*") -DestinationPath $sourceZip -Force

Write-Host "Saved $desktopZip"
Write-Host "Saved $sourceZip"
