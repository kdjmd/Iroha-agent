$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Resolve-Path (Join-Path $scriptDir "..")
$androidDir = Join-Path $projectRoot.Path "android"
$toolRoot = Join-Path $projectRoot.Path ".toolchain"
$downloadDir = Join-Path $toolRoot "downloads"
$jdkRoot = Join-Path $toolRoot "jdk"
$gradleRoot = Join-Path $toolRoot "gradle"
$sdkRoot = Join-Path $toolRoot "android-sdk"
$outputs = Resolve-Path (Join-Path $projectRoot.Path "..\..\outputs")

New-Item -ItemType Directory -Force -Path $downloadDir, $jdkRoot, $gradleRoot, $sdkRoot | Out-Null

function Download-IfMissing($Url, $Path) {
  if (Test-Path -LiteralPath $Path) {
    Write-Host "Using cached $Path"
    return
  }
  Write-Host "Downloading $Url"
  Invoke-WebRequest -Uri $Url -OutFile $Path
}

function Ensure-Under($Path, $Parent) {
  $resolvedPath = [System.IO.Path]::GetFullPath($Path)
  $resolvedParent = [System.IO.Path]::GetFullPath($Parent)
  if (-not $resolvedPath.StartsWith($resolvedParent, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Path escapes expected directory: $resolvedPath"
  }
}

$jdkZip = Join-Path $downloadDir "microsoft-jdk-17-windows-x64.zip"
$gradleZip = Join-Path $downloadDir "gradle-8.9-bin.zip"
$cmdlineZip = Join-Path $downloadDir "android-commandlinetools-win.zip"

Download-IfMissing "https://aka.ms/download-jdk/microsoft-jdk-17-windows-x64.zip" $jdkZip
Download-IfMissing "https://services.gradle.org/distributions/gradle-8.9-bin.zip" $gradleZip
Download-IfMissing "https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip" $cmdlineZip

if (-not (Get-ChildItem -LiteralPath $jdkRoot -Directory -Filter "jdk-*" -ErrorAction SilentlyContinue | Select-Object -First 1)) {
  Write-Host "Extracting JDK"
  Expand-Archive -LiteralPath $jdkZip -DestinationPath $jdkRoot -Force
}

if (-not (Get-ChildItem -LiteralPath $gradleRoot -Directory -Filter "gradle-*" -ErrorAction SilentlyContinue | Select-Object -First 1)) {
  Write-Host "Extracting Gradle"
  Expand-Archive -LiteralPath $gradleZip -DestinationPath $gradleRoot -Force
}

$cmdlineLatest = Join-Path $sdkRoot "cmdline-tools\latest"
$sdkManager = Join-Path $cmdlineLatest "bin\sdkmanager.bat"
if (-not (Test-Path -LiteralPath $sdkManager)) {
  Write-Host "Extracting Android command line tools"
  $tempCmdline = Join-Path $toolRoot "cmdline-extract"
  Ensure-Under $tempCmdline $toolRoot
  New-Item -ItemType Directory -Force -Path $tempCmdline | Out-Null
  Expand-Archive -LiteralPath $cmdlineZip -DestinationPath $tempCmdline -Force
  New-Item -ItemType Directory -Force -Path $cmdlineLatest | Out-Null
  $inner = Join-Path $tempCmdline "cmdline-tools"
  Get-ChildItem -LiteralPath $inner -Force | ForEach-Object {
    $target = Join-Path $cmdlineLatest $_.Name
    Ensure-Under $target $sdkRoot
    Move-Item -LiteralPath $_.FullName -Destination $target -Force
  }
}

$jdk = Get-ChildItem -LiteralPath $jdkRoot -Directory -Filter "jdk-*" | Select-Object -First 1
$gradle = Get-ChildItem -LiteralPath $gradleRoot -Directory -Filter "gradle-*" | Select-Object -First 1
if (-not $jdk) { throw "JDK extraction failed" }
if (-not $gradle) { throw "Gradle extraction failed" }

$env:JAVA_HOME = $jdk.FullName
$env:ANDROID_HOME = $sdkRoot
$env:ANDROID_SDK_ROOT = $sdkRoot
$env:Path = (Join-Path $jdk.FullName "bin") + ";" + (Join-Path $gradle.FullName "bin") + ";" + (Join-Path $sdkRoot "platform-tools") + ";" + $env:Path

Write-Host "Installing Android SDK packages"
1..20 | ForEach-Object { "y" } | & $sdkManager --sdk_root=$sdkRoot --licenses | Out-Host
& $sdkManager --sdk_root=$sdkRoot "platform-tools" "platforms;android-35" "build-tools;35.0.0"

Write-Host "Building Android APK"
& (Join-Path $gradle.FullName "bin\gradle.bat") -p $androidDir --no-daemon :app:assembleDebug

$apk = Join-Path $androidDir "app\build\outputs\apk\debug\app-debug.apk"
if (-not (Test-Path -LiteralPath $apk)) {
  throw "APK was not produced: $apk"
}

$outApk = Join-Path $outputs.Path "IrohaAgent-Android-debug.apk"
Copy-Item -LiteralPath $apk -Destination $outApk -Force
Get-Item -LiteralPath $outApk | Format-List FullName,Length,LastWriteTime

