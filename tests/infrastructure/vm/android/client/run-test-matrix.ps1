[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $HostName,
    [Parameter(Mandatory = $true)] [string] $AppApk,
    [Parameter(Mandatory = $true)] [string] $TestApk,
    [string] $UserName = "neuro-test",
    [string] $MatrixPath = (Join-Path $PSScriptRoot "..\config\test-matrix.yaml"),
    [string] $TestClass,
    [string] $TestPhoneNumber,
    [string] $TestPin,
    [string] $TestOtpEndpoint,
    [string] $RemoteRoot = "/opt/neuro-test",
    [string] $ResultsDirectory = (Join-Path (Get-Location) "matrix-results"),
    [switch] $ShowEmulator,
    [switch] $StopOnFailure
)

$ErrorActionPreference = "Stop"

function Read-TestMatrix {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $ResolvedPath = (Resolve-Path -LiteralPath $Path).Path
    $Scenarios = [System.Collections.Generic.List[object]]::new()
    $Current = $null

    foreach ($RawLine in [System.IO.File]::ReadAllLines($ResolvedPath)) {
        $Line = $RawLine.TrimEnd()
        if ([string]::IsNullOrWhiteSpace($Line) -or $Line.TrimStart().StartsWith("#")) { continue }
        if ($Line -match '^\s{2}-\s+name:\s*([A-Za-z0-9._-]+)\s*$') {
            if ($null -ne $Current) { $Scenarios.Add([pscustomobject]$Current) }
            $Current = [ordered]@{ Name = $Matches[1] }
            continue
        }
        if ($Line -match '^\s{4}([a-z_]+):\s*(.*?)\s*$' -and $null -ne $Current) {
            $Current[$Matches[1]] = $Matches[2].Trim('"', "'")
            continue
        }
        if ($Line -ne 'scenarios:') { throw "Unsupported matrix line: $RawLine" }
    }
    if ($null -ne $Current) { $Scenarios.Add([pscustomobject]$Current) }
    if ($Scenarios.Count -eq 0) { throw "The matrix does not contain any scenarios." }
    return $Scenarios
}

function Read-BooleanValue {
    param([object] $Value, [bool] $DefaultValue)
    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) { return $DefaultValue }
    switch ([string]$Value) {
        "true" { return $true }
        "false" { return $false }
        default { throw "Expected true or false, received '$Value'." }
    }
}

$SingleRunner = Join-Path $PSScriptRoot "run-remote-tests.ps1"
if (-not (Test-Path -LiteralPath $SingleRunner -PathType Leaf)) { throw "Single-device runner not found: $SingleRunner" }
$ResolvedAppApk = (Resolve-Path -LiteralPath $AppApk).Path
$ResolvedTestApk = (Resolve-Path -LiteralPath $TestApk).Path
$Remote = "$UserName@$HostName"
$MatrixRunId = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ")
$MatrixResults = Join-Path $ResultsDirectory $MatrixRunId
[void](New-Item -ItemType Directory -Path $MatrixResults -Force)
$Summary = [System.Collections.Generic.List[object]]::new()
$Scenarios = Read-TestMatrix -Path $MatrixPath

foreach ($Scenario in $Scenarios) {
    if (-not (Read-BooleanValue -Value $Scenario.enabled -DefaultValue $true)) {
        Write-Host "Skipping disabled scenario: $($Scenario.Name)"
        continue
    }
    if ([string]::IsNullOrWhiteSpace($Scenario.api_level) -or [string]::IsNullOrWhiteSpace($Scenario.device_profile)) {
        throw "Scenario '$($Scenario.Name)' requires api_level and device_profile."
    }

    $ScenarioTestClass = if ([string]::IsNullOrWhiteSpace($TestClass)) { [string]$Scenario.test_class } else { $TestClass }
    $RunnerParameters = @{
        HostName = $HostName
        UserName = $UserName
        AppApk = $ResolvedAppApk
        TestApk = $ResolvedTestApk
        ApiLevel = [int]$Scenario.api_level
        DeviceProfile = [string]$Scenario.device_profile
        SystemImage = if ([string]::IsNullOrWhiteSpace($Scenario.system_image)) { "google_apis" } else { [string]$Scenario.system_image }
        SystemImageAbi = if ([string]::IsNullOrWhiteSpace($Scenario.abi)) { "x86_64" } else { [string]$Scenario.abi }
        RemoteRoot = $RemoteRoot
    }
    if (-not [string]::IsNullOrWhiteSpace($ScenarioTestClass)) { $RunnerParameters.TestClass = $ScenarioTestClass }
    if (-not [string]::IsNullOrWhiteSpace($TestPhoneNumber)) { $RunnerParameters.TestPhoneNumber = $TestPhoneNumber }
    if (-not [string]::IsNullOrWhiteSpace($TestPin)) { $RunnerParameters.TestPin = $TestPin }
    if (-not [string]::IsNullOrWhiteSpace($TestOtpEndpoint)) { $RunnerParameters.TestOtpEndpoint = $TestOtpEndpoint }
    if ($ShowEmulator.IsPresent -or (Read-BooleanValue -Value $Scenario.show_emulator -DefaultValue $false)) {
        $RunnerParameters.ShowEmulator = $true
    }

    Write-Host "[$($Scenario.Name)] Submitting API $($Scenario.api_level) / $($Scenario.device_profile)"
    $SubmissionOutput = @(& $SingleRunner @RunnerParameters 2>&1)
    $SubmissionOutput | ForEach-Object { Write-Host $_ }
    $RunLine = $SubmissionOutput | Where-Object { "$_" -match '^Run ID:\s*(\S+)\s*$' } | Select-Object -Last 1
    if ($null -eq $RunLine -or "$RunLine" -notmatch '^Run ID:\s*(\S+)\s*$') { throw "Could not read the remote run ID." }
    $RunId = $Matches[1]

    Write-Host "[$($Scenario.Name)] Waiting for $RunId"
    $WaitCommand = "while [ `$(cat '$RemoteRoot/runs/$RunId/status') = running ]; do sleep 5; done; cat '$RemoteRoot/runs/$RunId/status'"
    $StatusOutput = @(& ssh $Remote $WaitCommand 2>&1)
    $Status = "$($StatusOutput | Select-Object -Last 1)".Trim()
    if ($Status -notin @("passed", "failed")) { throw "Unexpected status for ${RunId}: $Status" }

    $ScenarioResults = Join-Path $MatrixResults $Scenario.Name
    [void](New-Item -ItemType Directory -Path $ScenarioResults -Force)
    & scp -r "${Remote}:$RemoteRoot/runs/$RunId/results/." $ScenarioResults
    $DownloadExitCode = $LASTEXITCODE
    $Summary.Add([pscustomobject]@{
        Scenario = $Scenario.Name
        ApiLevel = [int]$Scenario.api_level
        DeviceProfile = [string]$Scenario.device_profile
        RunId = $RunId
        Status = $Status
        ResultsDownloaded = ($DownloadExitCode -eq 0)
    })
    Write-Host "[$($Scenario.Name)] $Status"
    if ($Status -eq "failed" -and $StopOnFailure.IsPresent) { break }
}

$SummaryPath = Join-Path $MatrixResults "summary.csv"
$Summary | Export-Csv -LiteralPath $SummaryPath -NoTypeInformation -Encoding UTF8
$Summary | Format-Table -AutoSize
Write-Output "Matrix results: $MatrixResults"
Write-Output "Summary: $SummaryPath"
if (($Summary | Where-Object Status -eq "failed").Count -gt 0) { exit 1 }