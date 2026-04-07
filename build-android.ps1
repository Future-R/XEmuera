param(
	[string]$Configuration = "Debug",
	[string]$Project = "D:\repo\XEmuera\XEmuera\XEmuera.Android\XEmuera.Android.csproj"
)

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

$msbuild = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $msbuild) {
	throw "Visual Studio MSBuild.exe not found. Install Xamarin Android support in Visual Studio first."
}

Write-Host "Using MSBuild:" $msbuild
Write-Host "Building:" $Project
& $msbuild $Project /restore /t:Build /p:Configuration=$Configuration
exit $LASTEXITCODE
