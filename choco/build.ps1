# YesNt Chocolatey Build & Pack Automation Script

$ErrorActionPreference = "Stop"

$nuspecPath = "yesnt.nuspec"
$projectRoot = Get-Location

Write-Host "--- Step 1: Building Projects in Release Mode ---" -ForegroundColor Cyan
dotnet build ../src/YesNt.Interpreter.App/YesNt.Interpreter.App.csproj -c Release
dotnet build ../src/YesNt.CodeEditor/YesNt.CodeEditor.csproj -c Release

Write-Host "`n--- Step 2: Packing Chocolatey Package ---" -ForegroundColor Cyan
# Remove old nupkg files if they exist
Get-ChildItem *.nupkg | Remove-Item -Force -ErrorAction SilentlyContinue

choco pack $nuspecPath

$package = Get-ChildItem *.nupkg | Select-Object -First 1

if ($package) {
    Write-Host "`nSuccessfully created: $($package.Name)" -ForegroundColor Green
    
    $push = Read-Host "`nDo you want to push '$($package.Name)' to Chocolatey? (y/N)"
    if ($push -eq 'y') {
        $apiKey = Read-Host "Enter your Chocolatey API Key (or press enter if already configured)"
        if ($apiKey) {
            choco push $($package.Name) --source "'https://push.chocolatey.org/'" --api-key $apiKey
        }
        else {
            choco push $($package.Name) --source "'https://push.chocolatey.org/'"
        }
    }
}
else {
    Write-Error "Failed to generate .nupkg file."
}
