[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $HostName,
    [Parameter(Mandatory = $true)] [string] $AppApk,
    [Parameter(Mandatory = $true)] [string] $TestApk,
    [Parameter(Mandatory = $true)] [string] $SerialA,
    [Parameter(Mandatory = $true)] [string] $SerialB,
    [Parameter(Mandatory = $true)] [string] $PinA,
    [Parameter(Mandatory = $true)] [string] $PinB,
    [ValidateSet("mutual-contacts")] [string] $Scenario = "mutual-contacts",
    [string] $UserName = "neuro-test",
    [string] $RemoteRoot = "/opt/neuro-test",
    [ValidateRange(1, 65535)] [int] $SshPort = 22,
    [string] $IdentityFile,
    [switch] $Wait,
    [string] $ResultsDirectory
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\RemoteClient.Common.ps1"

if ($SerialA -notmatch '^[A-Za-z0-9._:-]+$' -or $SerialB -notmatch '^[A-Za-z0-9._:-]+$') {
    throw "Device serials contain unsupported characters."
}
if ($SerialA -eq $SerialB) { throw "SerialA and SerialB must identify different devices." }
if ($PinA -notmatch '^\d{6}$' -or $PinB -notmatch '^\d{6}$') { throw "PinA and PinB must contain exactly six digits." }

$Context = New-RemoteClientContext -HostName $HostName -UserName $UserName -RemoteRoot $RemoteRoot `
    -SshPort $SshPort -IdentityFile $IdentityFile
$ResolvedAppApk = (Resolve-Path -LiteralPath $AppApk).Path
$ResolvedTestApk = (Resolve-Path -LiteralPath $TestApk).Path
$RunId = New-RemoteRunId
$EnvironmentLines = @(
    "MODE='$Scenario'"
    "SERIAL_A='$SerialA'"
    "SERIAL_B='$SerialB'"
    "TEST_PIN_A='$PinA'"
    "TEST_PIN_B='$PinB'"
)
$TemporaryEnvironment = Join-Path ([System.IO.Path]::GetTempPath()) "$RunId-job.env"
[System.IO.File]::WriteAllText($TemporaryEnvironment, ($EnvironmentLines -join "`n") + "`n", [System.Text.UTF8Encoding]::new($false))
try {
    $Files = [ordered]@{
        'app.apk' = $ResolvedAppApk
        'tests.apk' = $ResolvedTestApk
        'job.env' = $TemporaryEnvironment
    }
    $Paths = Publish-RemoteTestJob -Context $Context -RunId $RunId -Files $Files
}
finally {
    Remove-Item -LiteralPath $TemporaryEnvironment -Force -ErrorAction SilentlyContinue
}
Complete-RemoteTestSubmission -Context $Context -RunId $RunId -Paths $Paths -Wait:$Wait -ResultsDirectory $ResultsDirectory