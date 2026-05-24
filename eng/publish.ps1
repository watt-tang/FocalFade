# FocalFade Publish Script
# Builds, tests, and publishes a self-contained portable ZIP

param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$SolutionRoot = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $SolutionRoot "src\FocalFade.App\FocalFade.App.csproj"
$ArtifactsDir = Join-Path $SolutionRoot "artifacts"
$PublishDir = Join-Path $ArtifactsDir "publish"

Write-Host "=== FocalFade Publish Script ===" -ForegroundColor Cyan
Write-Host "Runtime: $Runtime"
Write-Host "Configuration: $Configuration"

# Clean
Write-Host "`n[1/6] Cleaning..." -ForegroundColor Yellow
if (Test-Path $ArtifactsDir) { Remove-Item -Recurse -Force $ArtifactsDir }
New-Item -ItemType Directory -Path $ArtifactsDir -Force | Out-Null

# Restore
Write-Host "`n[2/6] Restoring..." -ForegroundColor Yellow
dotnet restore $SolutionRoot
if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

# Test
Write-Host "`n[3/6] Running tests..." -ForegroundColor Yellow
dotnet test $SolutionRoot -c $Configuration --no-restore --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

# Publish
Write-Host "`n[4/6] Publishing..." -ForegroundColor Yellow
dotnet publish $ProjectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=true `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

# Create ZIP
Write-Host "`n[5/6] Creating ZIP..." -ForegroundColor Yellow
$Version = (Select-Xml -Path (Join-Path $SolutionRoot "Directory.Build.props") -XPath "//Version").Node.InnerText
$ZipName = "FocalFade-$Runtime-portable.zip"
$ZipPath = Join-Path $ArtifactsDir $ZipName
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath

# Checksum
Write-Host "`n[6/6] Generating checksum..." -ForegroundColor Yellow
$Hash = (Get-FileHash $ZipPath -Algorithm SHA256).Hash
$HashFile = "$ZipPath.sha256"
Set-Content -Path $HashFile -Value $Hash

Write-Host "`n=== Done ===" -ForegroundColor Green
Write-Host "ZIP:       $ZipPath"
Write-Host "SHA256:    $Hash"
Write-Host "Version:   $Version"
