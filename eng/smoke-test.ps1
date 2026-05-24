# FocalFade Smoke Test
# Quick validation that the published build works

param(
    [string]$PublishDir = ".\artifacts\publish"
)

$ErrorActionPreference = "Stop"
$ExePath = Join-Path $PublishDir "FocalFade.exe"

Write-Host "=== FocalFade Smoke Test ===" -ForegroundColor Cyan

# Check exe exists
if (!(Test-Path $ExePath)) {
    Write-Host "FAIL: FocalFade.exe not found at $ExePath" -ForegroundColor Red
    exit 1
}

# Check exe is valid PE
$bytes = [System.IO.File]::ReadAllBytes($ExePath)
if ($bytes[0] -ne 0x4D -or $bytes[1] -ne 0x5A) {
    Write-Host "FAIL: Not a valid PE file" -ForegroundColor Red
    exit 1
}

$fileInfo = Get-Item $ExePath
Write-Host "EXE size: $([math]::Round($fileInfo.Length / 1MB, 1)) MB"
Write-Host "EXE path: $ExePath"

# Try launching and killing after 3 seconds
Write-Host "`nLaunching FocalFade..."
$proc = Start-Process -FilePath $ExePath -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 3

if ($proc.HasExited) {
    Write-Host "FAIL: Process exited immediately (exit code: $($proc.ExitCode))" -ForegroundColor Red
    exit 1
}

Write-Host "Process running (PID: $($proc.Id))"

# Kill it
Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host "`n=== Smoke Test PASSED ===" -ForegroundColor Green
