#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Matrix,
    [string]$ResultsDirectory = (Join-Path $PSScriptRoot '../../TestResults/mobile'),
    [switch]$ShowEmulator
)
$ErrorActionPreference = 'Stop'
if ($IsWindows) { throw 'Managed emulator provisioning runs on Linux/macOS; use run-device-matrix.ps1 for existing Windows devices.' }
if (!$env:NEUROACCESS_TEST_APP_PATH) { throw 'Set NEUROACCESS_TEST_APP_PATH to the prebuilt APK on this Appium host.' }
if ($env:NEUROACCESS_TEST_ALLOW_RESET -ne 'true') { throw 'Managed profiles require NEUROACCESS_TEST_ALLOW_RESET=true for dedicated test app storage.' }
$Runner = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../infrastructure/vm/android/runner'))
$Profiles = @(Get-Content -LiteralPath $Matrix -Raw | ConvertFrom-Json)
$Summary = [Collections.Generic.List[object]]::new()
$PreviousSerial = $env:NEUROACCESS_TEST_DEVICE_ID
$PreviousServer = $env:NEUROACCESS_TEST_APPIUM_URL
$PreviousReset = $env:NEUROACCESS_TEST_RESET_BEFORE_TEST
$PreviousPhone = $env:NEUROACCESS_TEST_PHONE_NUMBER
$PreviousTimestamp = $env:NEUROACCESS_TEST_REGISTRATION_USERNAME_TIMESTAMP
$Registrations = 0
$RunRoot = Join-Path $ResultsDirectory ('profiles-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $RunRoot -Force | Out-Null
try {
    foreach ($Profile in $Profiles) {
        if (!$Profile.enabled) { continue }
        if ($Profile.name -notmatch '^[a-zA-Z0-9][a-zA-Z0-9._-]*$') { throw 'Invalid profile name.' }
        $Serial = $null
        $Outcome = 'Failed'
        $ProfileResults = Join-Path $RunRoot $Profile.name
        New-Item -ItemType Directory -Path $ProfileResults -Force | Out-Null
        $AvdName = "neuro_appium_$($Profile.name)"
        try {
            if ($Profile.api_level -lt 24) {
                $Outcome = 'Blocked: below application minimum API 24'
                continue
            }
            $env:NEUROACCESS_TEST_APPIUM_URL = $PreviousServer
            if ($Profile.api_level -lt 26) {
                if (!$env:NEUROACCESS_TEST_APPIUM_URL_LEGACY) {
                    $Outcome = 'Blocked: API 24/25 requires a separate UiAutomator2 5.0.0 server'
                    continue
                }
                $env:NEUROACCESS_TEST_APPIUM_URL = $env:NEUROACCESS_TEST_APPIUM_URL_LEGACY
            }
            $Arguments = @('--api', [string]$Profile.api_level, '--device', $Profile.device_profile,
                '--image', $Profile.system_image, '--abi', $Profile.abi, '--name', $AvdName, '--results', $ProfileResults)
            if ($ShowEmulator) { $Arguments += '--show-window' }
            $Output = & bash (Join-Path $Runner 'prepare-emulator.sh') @Arguments
            $Serial = @($Output | Where-Object { $_ -match '^emulator-[0-9]+$' }) | Select-Object -Last 1
            if ($LASTEXITCODE -ne 0 -or !$Serial) { throw 'Emulator provisioning failed.' }
            $env:NEUROACCESS_TEST_DEVICE_ID = $Serial
            $env:NEUROACCESS_TEST_RESET_BEFORE_TEST = 'true'
            if ($Profile.test_filter -match 'Registration') {
                # Each registering profile uses the next phone number and its own username timestamp, so accounts never collide.
                if ($PreviousPhone -notmatch '^[0-9]+$') { throw 'Registration profiles need NEUROACCESS_TEST_PHONE_NUMBER (digits only) as the first number to use.' }
                $env:NEUROACCESS_TEST_PHONE_NUMBER = ([long]$PreviousPhone + $Registrations).ToString('D' + $PreviousPhone.Length)
                $env:NEUROACCESS_TEST_REGISTRATION_USERNAME_TIMESTAMP = $null
                $Registrations++
            }
            & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'run-tests.ps1') -Suite UI -Platform android `
                -Filter $Profile.test_filter -ResultsDirectory $ProfileResults
            if ($LASTEXITCODE -eq 0) { $Outcome = 'Passed' }
        } catch {
            Write-Warning "Profile $($Profile.name) failed; inspect its protected results directory."
        } finally {
            if ($Serial) {
                & bash (Join-Path $Runner 'stop-emulator.sh') $Serial
                if ($LASTEXITCODE -ne 0) { $Outcome = 'Failed: emulator cleanup' }
                elseif ($Profile.storage_policy -eq 'delete') {
                    & bash (Join-Path $Runner 'remove-managed-emulator.sh') --name $AvdName --results $ProfileResults
                    if ($LASTEXITCODE -ne 0) { $Outcome = 'Failed: AVD cleanup' }
                }
            }
            $Summary.Add([ordered]@{ profile = $Profile.name; outcome = $Outcome })
        }
    }
} finally {
    $env:NEUROACCESS_TEST_DEVICE_ID = $PreviousSerial
    $env:NEUROACCESS_TEST_APPIUM_URL = $PreviousServer
    $env:NEUROACCESS_TEST_RESET_BEFORE_TEST = $PreviousReset
    $env:NEUROACCESS_TEST_PHONE_NUMBER = $PreviousPhone
    $env:NEUROACCESS_TEST_REGISTRATION_USERNAME_TIMESTAMP = $PreviousTimestamp
    ConvertTo-Json -InputObject $Summary.ToArray() | Set-Content -LiteralPath (Join-Path $RunRoot 'profiles.json')
}
if ($Summary.Count -eq 0 -or @($Summary | Where-Object { $_.outcome -ne 'Passed' }).Count -gt 0) { exit 1 }
