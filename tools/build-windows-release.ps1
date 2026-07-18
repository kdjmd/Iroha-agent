[CmdletBinding()]
param(
  [string]$Version = "2.3.0",
  [string]$OutputRoot = (Join-Path ([Environment]::GetFolderPath("Desktop")) "IrohaAgent-GitHub-Releases"),
  [switch]$FullVoice,
  [string]$RuntimeArchive = $env:IROHA_RUNTIME_ARCHIVE,
  [string]$VoicePackage = $env:IROHA_VOICE_PACKAGE,
  [switch]$KeepStaging,
  [switch]$SkipQa
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$desktopRoot = Join-Path $projectRoot "desktop"
$dist = Join-Path $desktopRoot "dist"
$releaseRoot = [IO.Path]::GetFullPath($OutputRoot)
$releaseDirectory = Join-Path $releaseRoot ("v" + $Version)
$stagingRoot = Join-Path $env:TEMP ("IrohaAgent-Release-{0}-{1}" -f $Version, $PID)
$portableName = "IrohaAgent-Windows-v$Version-Portable"
$fullName = "IrohaAgent-Windows-v$Version-FullVoice"
$portableStage = Join-Path $stagingRoot $portableName
$fullStage = Join-Path $stagingRoot $fullName

function Remove-SafeDirectory([string]$Path, [string]$RequiredParent) {
  if (-not (Test-Path -LiteralPath $Path)) { return }
  $fullPath = [IO.Path]::GetFullPath($Path).TrimEnd('\')
  $fullParent = [IO.Path]::GetFullPath($RequiredParent).TrimEnd('\') + '\'
  if (-not $fullPath.StartsWith($fullParent, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to remove a directory outside the expected parent: $fullPath"
  }
  Remove-Item -LiteralPath $fullPath -Recurse -Force
}

function Resolve-SevenZip {
  foreach ($candidate in @(
    (Join-Path $env:ProgramFiles "7-Zip\7z.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "7-Zip\7z.exe")
  )) {
    if ($candidate -and (Test-Path -LiteralPath $candidate)) { return $candidate }
  }
  throw "7-Zip was not found. Install 7-Zip or provide it in Program Files."
}

function Write-FirstUseGuide([string]$Directory, [bool]$IncludesVoice) {
  $voiceText = if ($IncludesVoice) {
    @(
      "This package includes the complete GPT-SoVITS runtime and the configured voice model.",
      "The first launch extracts, matches, and starts the voice service with live progress feedback.",
      "Keep at least 20 GB of free disk space and leave the app open until setup completes.",
      "Later launches connect automatically without extracting the runtime again."
    ) -join [Environment]::NewLine
  } else {
    @(
      "This package does not include the large GPT-SoVITS runtime. Text chat works immediately.",
      "The app can discover an existing GPT-SoVITS install, or use IROHA_GPT_SOVITS_ROOT.",
      "Download every FullVoice volume when a self-contained voice setup is required."
    ) -join [Environment]::NewLine
  }

  $content = @(
    "Iroha Agent v$Version - Windows",
    "",
    "1. Extract the complete folder. Do not run the EXE from inside the archive.",
    "2. Run IrohaAgent.exe.",
    "3. Open Settings, choose a provider and model, then enter your own provider API key.",
    "4. Provider keys are protected with Windows CurrentUser DPAPI and are never included in this release.",
    "   On another PC or Windows account, enter the provider key again.",
    "5. Open Tools & Privacy to choose capability bundles, authorized folders, apps, and work styles.",
    "6. Write, delete, clipboard, image, media, and app actions always ask for confirmation.",
    "   The app saves email drafts locally but never sends email or runs arbitrary shell commands.",
    "",
    $voiceText,
    "If voice setup fails, open Settings and click Redeploy Voice.",
    "Redeploy only replaces app-managed copies. Source packages and external GPT-SoVITS folders are preserved.",
    "",
    "This is an unofficial fan-made technical project. Confirm redistribution rights before sharing character, visual, or voice assets."
  ) -join [Environment]::NewLine
  Set-Content -LiteralPath (Join-Path $Directory "README-FIRST-USE.txt") -Value $content -Encoding UTF8
}

function Copy-App([string]$Destination, [bool]$IncludesVoice) {
  New-Item -ItemType Directory -Force -Path $Destination | Out-Null
  foreach ($releaseItem in @("IrohaAgent.exe", "Start-IrohaAgent.bat", "assets")) {
    $source = Join-Path $dist $releaseItem
    if (-not (Test-Path -LiteralPath $source)) {
      throw "Required release item is missing from desktop/dist: $releaseItem"
    }
    Copy-Item -LiteralPath $source -Destination $Destination -Recurse -Force
  }
  Get-ChildItem -LiteralPath $dist -Filter "*.dll" -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $Destination -Force
  }
  foreach ($notice in @(
    @{ Source=(Join-Path $projectRoot "LICENSE"); Name="LICENSE" },
    @{ Source=(Join-Path $projectRoot "THIRD_PARTY_NOTICES.md"); Name="THIRD_PARTY_NOTICES.md" },
    @{ Source=(Join-Path $projectRoot "docs\ASSET_NOTICE.md"); Name="ASSET_NOTICE.md" }
  )) {
    if (-not (Test-Path -LiteralPath $notice.Source)) { throw "Required release notice is missing: $($notice.Source)" }
    Copy-Item -LiteralPath $notice.Source -Destination (Join-Path $Destination $notice.Name) -Force
  }
  Write-FirstUseGuide $Destination $IncludesVoice
  $forbiddenNames = @(
    "settings.json", "memory.json", "reminders.json", "calendar.json",
    "email-drafts.json", "knowledge-base.json", "crash.log", ".env"
  )
  $forbiddenFiles = Get-ChildItem -LiteralPath $Destination -Recurse -Force -File |
    Where-Object { $forbiddenNames -contains $_.Name }
  if ($forbiddenFiles) {
    throw "Release staging unexpectedly contains user data: $($forbiddenFiles.FullName -join ', ')"
  }
}

if ($SkipQa) {
  & (Join-Path $desktopRoot "build.ps1")
} else {
  & (Join-Path $PSScriptRoot "run-regression-qa.ps1") -OutputRoot (Join-Path $stagingRoot "regression-qa")
}
if (-not (Test-Path -LiteralPath (Join-Path $dist "IrohaAgent.exe"))) {
  throw "Windows build did not produce IrohaAgent.exe"
}
$builtVersion = (Get-Item -LiteralPath (Join-Path $dist "IrohaAgent.exe")).VersionInfo.ProductVersion
if ($builtVersion -ne $Version) {
  throw "Built executable version $builtVersion does not match release version $Version"
}

New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null
Remove-SafeDirectory $releaseDirectory $releaseRoot
Remove-SafeDirectory $stagingRoot $env:TEMP
New-Item -ItemType Directory -Force -Path $releaseDirectory, $stagingRoot | Out-Null

try {
  Copy-App $portableStage $false
  $portableZip = Join-Path $releaseDirectory ($portableName + ".zip")
  Compress-Archive -LiteralPath $portableStage -DestinationPath $portableZip -CompressionLevel Optimal

  if ($FullVoice) {
    if ([string]::IsNullOrWhiteSpace($RuntimeArchive)) {
      throw "-FullVoice requires -RuntimeArchive or IROHA_RUNTIME_ARCHIVE."
    }
    if ([string]::IsNullOrWhiteSpace($VoicePackage)) {
      throw "-FullVoice requires -VoicePackage or IROHA_VOICE_PACKAGE."
    }
    $RuntimeArchive = (Resolve-Path -LiteralPath $RuntimeArchive).Path
    $VoicePackage = (Resolve-Path -LiteralPath $VoicePackage).Path
    $sevenZip = Resolve-SevenZip

    Copy-App $fullStage $true
    $voiceRuntime = Join-Path $fullStage "voice-runtime"
    $voiceTools = Join-Path $voiceRuntime "tools"
    New-Item -ItemType Directory -Force -Path $voiceTools | Out-Null

    if ($RuntimeArchive.EndsWith(".7z.001", [StringComparison]::OrdinalIgnoreCase)) {
      $runtimePrefix = $RuntimeArchive.Substring(0, $RuntimeArchive.Length - 3)
      Get-ChildItem -LiteralPath (Split-Path $RuntimeArchive -Parent) -File |
        Where-Object { $_.FullName.StartsWith($runtimePrefix, [StringComparison]::OrdinalIgnoreCase) } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $voiceRuntime -Force }
    } else {
      Copy-Item -LiteralPath $RuntimeArchive -Destination $voiceRuntime -Force
    }
    Copy-Item -LiteralPath $VoicePackage -Destination (Join-Path $voiceRuntime "iroha-model.zip") -Force
    Copy-Item -LiteralPath $sevenZip -Destination (Join-Path $voiceTools "7z.exe") -Force
    foreach ($supportFile in @("7z.dll", "License.txt")) {
      $source = Join-Path (Split-Path $sevenZip -Parent) $supportFile
      if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $voiceTools -Force }
    }

    Push-Location $stagingRoot
    try {
      $fullArchive = Join-Path $releaseDirectory ($fullName + ".7z")
      & $sevenZip a -t7z -mx=0 -v1900m $fullArchive $fullName
      if ($LASTEXITCODE -ne 0) { throw "7-Zip release packaging failed with exit code $LASTEXITCODE" }
    } finally {
      Pop-Location
    }
  }

  $releaseNotes = @(
    "Iroha Agent v$Version - Windows-only Release",
    "",
    "- Portable.zip: approximately 30 MB and does not include the large voice runtime.",
    "- FullVoice.7z.001 and later volumes: download all parts and extract the .001 file with 7-Zip.",
    "- Android APK is intentionally not included in this release.",
    "- Release files contain no API key, chat history, memory, or user settings.",
    "- v2.3.0 adds 18 permission-controlled Tools and 10 optional Skills in capability bundles A, B, and C.",
    "- Native tool calling is adapted for OpenAI Responses, OpenAI/DeepSeek Chat, Anthropic, Gemini, and Cohere.",
    "- Search, memory, reminders, document/PDF reading, local knowledge, weather, calendar, drafts, image analysis, media controls, and app allowlists are included.",
    "- Writes, deletion, clipboard, images, media keys, and app launches require one-shot confirmation.",
    "- The release never sends email, runs arbitrary shell commands, or deletes arbitrary files.",
    "- Provider and Brave Search keys use Windows CurrentUser DPAPI and legacy plaintext settings migrate automatically.",
    "- Remote HTTP endpoints carrying keys are rejected and provider errors are redacted.",
    "- Provider API keys, selected models, and custom endpoints are isolated per provider and migrate from legacy DeepSeek settings.",
    "- Settings now includes a dedicated Redeploy Voice action with progress feedback and source-package protection.",
    "- Confirm redistribution rights before uploading the FullVoice assets."
  ) -join [Environment]::NewLine
  Set-Content -LiteralPath (Join-Path $releaseDirectory "RELEASE_NOTES.txt") -Value $releaseNotes -Encoding UTF8

  $hashLines = Get-ChildItem -LiteralPath $releaseDirectory -File |
    Where-Object { $_.Name -ne "SHA256SUMS.txt" } |
    Sort-Object Name |
    ForEach-Object {
      $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
      "$hash *$($_.Name)"
    }
  Set-Content -LiteralPath (Join-Path $releaseDirectory "SHA256SUMS.txt") -Value $hashLines -Encoding ASCII

  Write-Host "Windows release prepared at $releaseDirectory"
  Get-ChildItem -LiteralPath $releaseDirectory -File | Select-Object Name, Length
} finally {
  if (-not $KeepStaging) {
    Remove-SafeDirectory $stagingRoot $env:TEMP
  } else {
    Write-Host "Staging kept at $stagingRoot"
  }
}
