param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9-]+$')]
    [string]$RunId,
    [ValidateSet('All', 'ReleaseRemaining')]
    [string]$Stage = 'All'
)

$ErrorActionPreference = 'Stop'
$taskRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $taskRoot
$statusPath = Join-Path $taskRoot "Test/$RunId-status.json"
$gates = if ($Stage -eq 'ReleaseRemaining') { @('ReleaseCorpus', 'ReleaseUI', 'ReleaseRealText') }
    else { @('DebugCore', 'DebugUI', 'ReleaseCorpus', 'ReleaseUI', 'ReleaseRealText') }
if (Test-Path -LiteralPath $statusPath) { throw "A status already exists for $RunId" }
foreach ($gate in $gates) {
    foreach ($extension in @('log', 'json')) {
        if (Test-Path -LiteralPath (Join-Path $taskRoot "Test/$RunId-$gate.$extension")) {
            throw "An earlier gate artifact already exists for $RunId/$gate"
        }
    }
}
$state = [ordered]@{ RunId=$RunId; Stage=$Stage; ProcessId=$PID; Started=[DateTime]::UtcNow.ToString('o'); State='Running'; Gate=$null; Results=@(); Error=$null }
function Save-State {
    $temporary = "$statusPath.tmp"
    $state | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $temporary -Encoding UTF8
    Move-Item -LiteralPath $temporary -Destination $statusPath -Force
}
try {
    foreach ($gate in $gates) {
        $state.Gate = $gate; Save-State
        $logPath = Join-Path $taskRoot "Test/$RunId-$gate.log"
        # Windows PowerShell wraps redirected native stderr in error records.
        # Expected corpus refusals are output; only the actual exit decides
        # gate success. Keep strict error handling for runner/file operations.
        $ErrorActionPreference = 'Continue'
        try {
            & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Run-RecordedGate.ps1') -Gate $gate -RunId $RunId > $logPath 2>&1
            $gateExit = $LASTEXITCODE
        } finally { $ErrorActionPreference = 'Stop' }
        $state.Results += [ordered]@{ Gate=$gate; Exit=$gateExit; Finished=[DateTime]::UtcNow.ToString('o') }
        Save-State
        if ($gateExit -ne 0 -and -not ($gate -eq 'ReleaseCorpus' -and $gateExit -eq 1)) {
            throw "Gate $gate failed with exit $gateExit; see $logPath"
        }
    }
    $state.State = 'Completed'; $state.Gate = $null; Save-State
    exit 0
} catch {
    $state.State = 'Failed'; $state.Error = $_.Exception.Message; Save-State
    exit 1
}
