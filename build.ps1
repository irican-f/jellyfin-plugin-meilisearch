param(
    [string]$Version = "0.0.0-dev",
    [string]$Output = "./artifacts"
)

$ErrorActionPreference = "Stop"

# Clean output directory
if (Test-Path $Output) {
    Remove-Item -Recurse -Force $Output
}
New-Item -ItemType Directory -Path $Output | Out-Null

# Update version in build.yaml
$buildYaml = Get-Content ./src/build.yaml -Raw
$buildYaml = $buildYaml -replace "version: .*", "version: '$Version'"
Set-Content ./src/build.yaml $buildYaml

Write-Host "Building plugin version $Version..." -ForegroundColor Cyan

# Ensure jprm is installed
python -m pip install --quiet --upgrade jprm

# Build with jprm
python -m jprm plugin build ./src --output $Output --dotnet-framework net9.0

Write-Host "Done! Output in $Output" -ForegroundColor Green
Get-ChildItem $Output
