[CmdletBinding()]
param(
    [switch]$Validate,
    [string]$CorpusRoot
)
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $PSScriptRoot
try {
    & dotnet build 'DK2MapEditor.slnx' --configuration Release -p:UseSharedCompilation=false -m:1 -nodeReuse:false
    if ($LASTEXITCODE -ne 0) { throw "Solution build failed ($LASTEXITCODE)." }
    & dotnet publish 'src/DK2MapEditor.App/DK2MapEditor.App.csproj' --configuration Release --no-build --no-restore --output 'artifacts/PreFinal/App'
    if ($LASTEXITCODE -ne 0) { throw "App publication failed ($LASTEXITCODE)." }
    # Keep internal assembly/namespace names stable; the apphost still loads DK2MapEditor.App.dll.
    Move-Item -LiteralPath 'artifacts/PreFinal/App/DK2MapEditor.App.exe' -Destination 'artifacts/PreFinal/App/UltimateEditor.exe' -Force
    & dotnet publish 'src/DK2MapEditor.Mcp/DK2MapEditor.Mcp.csproj' --configuration Release --no-build --no-restore --output 'artifacts/PreFinal/Mcp'
    if ($LASTEXITCODE -ne 0) { throw "MCP publication failed ($LASTEXITCODE)." }
    Copy-Item -LiteralPath 'distribution/Start-Editor-MCP.cmd' -Destination 'artifacts/PreFinal/Start-Editor-MCP.cmd' -Force
    New-Item -ItemType Directory -Force -Path 'artifacts/PreFinal/Documentation' | Out-Null
    Get-ChildItem -LiteralPath 'docs' -File | Where-Object Extension -In '.md', '.json' | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination 'artifacts/PreFinal/Documentation' -Force
    }
    foreach ($documentName in 'NOTICE.md', 'CONTRIBUTING.md') {
        Copy-Item -LiteralPath $documentName -Destination 'artifacts/PreFinal' -Force
    }
    Copy-Item -LiteralPath 'distribution/README.md' -Destination 'artifacts/PreFinal/README.md' -Force
    Copy-Item -LiteralPath 'distribution/README.md' -Destination 'artifacts/PreFinal/README.txt' -Force
    if ($Validate) {
        $validation = 'tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll'
        $validationReport = Join-Path 'artifacts' ('validation-{0}.json' -f (Get-Date -Format 'yyyyMMdd-HHmmssfff'))
        if ($CorpusRoot) {
            & dotnet $validation $CorpusRoot --report $validationReport
        } else {
            & dotnet $validation --report $validationReport
        }
        if ($LASTEXITCODE -ne 0) { throw "Validation reported failures ($LASTEXITCODE); see $validationReport." }
    }
    Write-Host "Editor: $(Join-Path $PSScriptRoot 'artifacts/PreFinal/App/UltimateEditor.exe')"
} finally {
    Pop-Location
}
