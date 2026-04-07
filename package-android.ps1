param(
	[string]$Configuration = "Release",
	[string]$Project = "D:\repo\XEmuera\XEmuera\XEmuera.Android\XEmuera.Android.csproj",
	[string]$OutputDir = "D:\repo\XEmuera\artifacts\android",
	[string]$PackageName = "XEmuera-android"
)

function Resolve-MSBuildPath {
	$candidatePaths = @(
		"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\2022\Preview\MSBuild\Current\Bin\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\18\Preview\MSBuild\Current\Bin\MSBuild.exe"
	)

	return $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

function Get-LatestApk {
	param(
		[string]$SearchRoot
	)

	$preferred = Get-ChildItem $SearchRoot -Recurse -Filter "*-Signed.apk" -ErrorAction SilentlyContinue |
		Sort-Object LastWriteTime -Descending |
		Select-Object -First 1
	if ($preferred) {
		return $preferred
	}

	return Get-ChildItem $SearchRoot -Recurse -Filter "*.apk" -ErrorAction SilentlyContinue |
		Sort-Object LastWriteTime -Descending |
		Select-Object -First 1
}

$msbuild = Resolve-MSBuildPath
if (-not $msbuild) {
	throw "Visual Studio MSBuild.exe not found. Install Xamarin Android support in Visual Studio first."
}

if (-not (Test-Path $Project)) {
	throw "Android project not found: $Project"
}

$projectDirectory = Split-Path -Parent $Project
$binDirectory = Join-Path $projectDirectory "bin\$Configuration"

Write-Host "Using MSBuild:" $msbuild
Write-Host "Packaging:" $Project
Write-Host "Configuration:" $Configuration

& $msbuild $Project /restore /t:SignAndroidPackage /p:Configuration=$Configuration
if ($LASTEXITCODE -ne 0) {
	exit $LASTEXITCODE
}

$apk = Get-LatestApk -SearchRoot $binDirectory
if (-not $apk) {
	throw "Packaging finished but no APK was found under $binDirectory"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$artifactName = "{0}-{1}.apk" -f $PackageName, $Configuration
$artifactPath = Join-Path $OutputDir $artifactName
Copy-Item -LiteralPath $apk.FullName -Destination $artifactPath -Force

Write-Host ""
Write-Host "Package ready:"
Write-Host "  Source : $($apk.FullName)"
Write-Host "  Copied : $artifactPath"
Write-Host "  Size   : $([Math]::Round($apk.Length / 1MB, 2)) MB"
