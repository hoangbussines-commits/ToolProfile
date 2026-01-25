$ErrorActionPreference = "Stop"

Write-Host "========================================"
Write-Host "  TOOLPROFILE PUBLISH - FIXED"
Write-Host "========================================"
Write-Host ""

# Clean
Write-Host "Cleaning..."
dotnet clean

# Publish WITHOUT trimming
Write-Host "Publishing..."
dotnet publish `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output "./Publish"

Write-Host ""
Write-Host "========================================"
Write-Host "  DONE!"
Write-Host "========================================"
Write-Host ""
Write-Host "Output: $(Resolve-Path ./Publish)"

Read-Host "Press Enter to exit"
