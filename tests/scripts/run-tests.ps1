#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Unit', 'UI')][string]$Suite = 'Unit',
    [ValidateSet('android', 'ios')][string]$Platform = 'android',
    [string]$Settings,
    [string]$Filter,
    [string]$ResultsDirectory,
    [switch]$AllowIncomplete
)
$ErrorActionPreference = 'Stop'
$Suite = @{ unit = 'Unit'; ui = 'UI' }[$Suite.ToLowerInvariant()]
$Platform = $Platform.ToLowerInvariant()
$RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$Filter -and $Suite -eq 'UI') { $Filter = 'TestCategory=Smoke' }
if (!$Settings -and $Suite -eq 'UI') { $Settings = Join-Path $RepositoryRoot "tests/config/$Platform.runsettings" }
if (!$ResultsDirectory) { $ResultsDirectory = Join-Path $RepositoryRoot ($Suite -eq 'UI' ? 'TestResults/mobile' : 'TestResults/unit') }
$RunDirectory = Join-Path $ResultsDirectory ([DateTime]::UtcNow.ToString('yyyyMMddTHHmmss') + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $RunDirectory -Force | Out-Null
$PreviousEnabled = $env:NEUROACCESS_TEST_ENABLED
try {
    if ($Suite -eq 'UI') { $env:NEUROACCESS_TEST_ENABLED = 'true' }
    $Options = @()
    if ($Settings) { $Options = @('--settings', $Settings) }
    if ($Filter) { $Options += @('--filter', $Filter) }
    & dotnet test (Join-Path $RepositoryRoot "tests/NeuroAccessMaui.${Suite}Tests/NeuroAccessMaui.${Suite}Tests.csproj") `
        @Options --logger 'console;verbosity=normal' `
        --logger 'trx;LogFileName=results.trx' --results-directory $RunDirectory
    $RunExitCode = $LASTEXITCODE
    if ($RunExitCode -ne 0) { exit $RunExitCode }
    $ReportPath = Join-Path $RunDirectory 'results.trx'
    if (!(Test-Path -LiteralPath $ReportPath)) { throw 'The run produced no TRX report.' }
    [xml]$Report = Get-Content -LiteralPath $ReportPath -Raw
    $Counters = $Report.TestRun.ResultSummary.Counters
    if ([int]$Counters.total -eq 0) { throw 'No tests matched the requested filter.' }
    if (!$AllowIncomplete -and [int]$Counters.passed -ne [int]$Counters.total) {
        Write-Error 'The selected run is incomplete (blocked, skipped or failed tests). Inspect its TRX report.' -ErrorAction Continue
        exit 2
    }
    Write-Output "Test results: $ReportPath"
} finally {
    $env:NEUROACCESS_TEST_ENABLED = $PreviousEnabled
}
