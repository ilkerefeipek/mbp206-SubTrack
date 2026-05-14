$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$toolsDir = Join-Path $repoRoot 'tools'
New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
$outFile = Join-Path $toolsDir 'tailwindcss.exe'

# Pin to v3.4.17 (last v3 release). v4 introduces breaking CSS-first config.
$version = 'v3.4.17'
$url = "https://github.com/tailwindlabs/tailwindcss/releases/download/$version/tailwindcss-windows-x64.exe"

if (Test-Path $outFile) {
    $current = (& $outFile --help 2>&1 | Select-Object -First 1) -join ''
    if ($current -match $version.TrimStart('v')) {
        Write-Host "Tailwind $version already installed at $outFile"
        return
    }
    Write-Host "Replacing existing tailwindcss.exe (version mismatch)"
    Remove-Item $outFile -Force
}

Write-Host "Downloading $url ..."
Invoke-WebRequest -Uri $url -OutFile $outFile -UseBasicParsing
Unblock-File $outFile
Write-Host "Installed: $outFile"
& $outFile --help | Select-Object -First 1

# To run this script: powershell -ExecutionPolicy Bypass -File scripts/install-tailwind.ps1
