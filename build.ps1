param(
    [switch]$TestOnly
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Dist = Join-Path $Root "dist"
$CoreDir = Join-Path $Root "src\FishingGame.Core"
$AppDir = Join-Path $Root "src\FishingGame.WinForms"
$TestsDir = Join-Path $Root "tests"
$Csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $Csc)) {
    throw "C# compiler not found at $Csc"
}

New-Item -ItemType Directory -Force -Path $Dist | Out-Null

$CoreSources = @()
if (Test-Path $CoreDir) {
    $CoreSources = Get-ChildItem -Path $CoreDir -Filter "*.cs" | Sort-Object Name | ForEach-Object { $_.FullName }
}

$TestSource = Join-Path $TestsDir "CoreTests.cs"
$TestExe = Join-Path $Dist "CoreTests.exe"

& $Csc /nologo /target:exe /out:$TestExe /reference:System.Core.dll /reference:System.Web.Extensions.dll $CoreSources $TestSource
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $TestExe
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($TestOnly) {
    exit 0
}

$AppSources = @()
if (Test-Path $AppDir) {
    $AppSources = Get-ChildItem -Path $AppDir -Filter "*.cs" | Sort-Object Name | ForEach-Object { $_.FullName }
}

$AppExe = Join-Path $Dist "FishingGame.exe"
& $Csc /nologo /target:winexe /out:$AppExe /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll $CoreSources $AppSources
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Output "Built $AppExe"

