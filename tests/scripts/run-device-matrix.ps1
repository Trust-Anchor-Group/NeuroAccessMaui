#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Matrix,
    [string]$Filter = 'TestCategory=Smoke',
    [string]$ResultsDirectory = (Join-Path $PSScriptRoot '../../TestResults/mobile')
)
$ErrorActionPreference = 'Stop'
$Devices = @(Get-Content -LiteralPath $Matrix -Raw | ConvertFrom-Json -AsHashtable)
$AnyFailure = $false
$Names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($Device in $Devices) {
    if ($Device.name -notmatch '^[a-zA-Z0-9][a-zA-Z0-9._-]*$' -or !$Names.Add($Device.name)) { throw 'Matrix names must be unique safe directory names.' }
    if ($Device.platform -notin @('android', 'ios')) { throw 'Matrix platform must be android or ios.' }
    $Previous = @{}
    try {
        foreach ($Name in $Device.settings.Keys) {
            if ($Name -notmatch '^[A-Z][A-Z0-9_]*$') { throw 'Invalid matrix setting name.' }
            $Key = "NEUROACCESS_TEST_$Name"
            $Previous[$Key] = [Environment]::GetEnvironmentVariable($Key, 'Process')
            [Environment]::SetEnvironmentVariable($Key, [string]$Device.settings[$Name], 'Process')
        }
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'run-tests.ps1') -Suite UI -Platform $Device.platform `
            -Filter $Filter -ResultsDirectory (Join-Path $ResultsDirectory $Device.name)
        if ($LASTEXITCODE -ne 0) { $AnyFailure = $true }
    } finally {
        foreach ($Key in $Previous.Keys) { [Environment]::SetEnvironmentVariable($Key, $Previous[$Key], 'Process') }
    }
}
if ($Devices.Count -eq 0) { throw 'Device matrix is empty.' }
if ($AnyFailure) { exit 1 }
