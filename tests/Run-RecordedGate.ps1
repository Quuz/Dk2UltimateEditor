param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('DebugCore', 'DebugUI', 'ReleaseCorpus', 'ReleaseUI', 'ReleaseRealText')]
    [string]$Gate,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9-]+$')]
    [string]$RunId
)

$ErrorActionPreference = 'Stop'
$taskRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$env:TEMP = Join-Path $taskRoot 'Test/temp'
$env:TMP = $env:TEMP
Set-Location -LiteralPath $taskRoot
Write-Output "DK2_GATE_START=$Gate/$RunId"
try {
$configuration = if ($Gate.StartsWith('Debug')) { 'Debug' } else { 'Release' }
$gateArgs = if ($Gate.EndsWith('UI')) {
    $uiInputs = @(
        'D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Standart/Alcatraz.kwd'
        'D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Special/Kat/Ancients/Ancients.map'
    )
    foreach ($uiInput in $uiInputs) {
        if (-not (Test-Path -LiteralPath $uiInput -PathType Leaf)) {
            throw "Required real UI input is missing: $uiInput"
        }
    }
    @((Join-Path $taskRoot "tests/DK2MapEditor.WindowsSmoke/bin/$configuration/net10.0-windows/DK2MapEditor.WindowsSmoke.dll")) + $uiInputs
} else {
    $validationArgs = @(Join-Path $taskRoot "tests/DK2MapEditor.Validation/bin/$configuration/net10.0/DK2MapEditor.Validation.dll")
    if ($Gate -eq 'ReleaseCorpus') { $validationArgs += 'D:\Games\DK2\Dungeon Keeper 2\Data\editor\maps' }
    if ($Gate -eq 'ReleaseRealText') { $validationArgs += 'D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Special/Kat/Ancients/Ancients.map' }
    $gateReport = Join-Path $taskRoot "Test/$RunId-$Gate.json"
    if (Test-Path -LiteralPath $gateReport) {
        throw "A report already exists for this run: $gateReport"
    }
    $validationArgs += @('--report', $gateReport)
    $validationArgs
}
    # Keep a typed argument array even if a gate has a single input.
    $gateArgs = [string[]]$gateArgs
    & dotnet @gateArgs
    $gateExit = $LASTEXITCODE
    Write-Output "DK2_GATE_EXIT=$gateExit"
    exit $gateExit
} catch {
    Write-Error $_ -ErrorAction Continue
    Write-Output 'DK2_GATE_EXIT=runner-error'
    exit 99
}
