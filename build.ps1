$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $root "src\OpenDimmer"
$distDir = Join-Path $root "dist"

if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$fwDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$wpfDir = Join-Path $fwDir "WPF"

$sources = Get-ChildItem -Path $srcDir -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName
$manifest = Join-Path $srcDir "app.manifest"
$icoFile = Join-Path $root "assets\app.ico"
$outExe = Join-Path $distDir "OpenDimmer.exe"

Write-Host "Compiling OpenDimmer..." -ForegroundColor Cyan

& $csc /nologo /target:winexe /optimize+ `
    /out:$outExe `
    /win32manifest:$manifest `
    /win32icon:"$icoFile" `
    /resource:"$icoFile,OpenDimmer.app.ico" `
    /r:"$fwDir\System.dll" `
    /r:"$fwDir\System.Core.dll" `
    /r:"$fwDir\System.Drawing.dll" `
    /r:"$fwDir\System.Windows.Forms.dll" `
    /r:"$fwDir\System.Web.Extensions.dll" `
    /r:"$fwDir\System.Xaml.dll" `
    /r:"$wpfDir\WindowsBase.dll" `
    /r:"$wpfDir\PresentationCore.dll" `
    /r:"$wpfDir\PresentationFramework.dll" `
    $sources

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build succeeded: $outExe" -ForegroundColor Green
} else {
    Write-Error "Build failed with exit code $LASTEXITCODE"
}
