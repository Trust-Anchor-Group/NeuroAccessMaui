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
    [string] $RemoteRoot = "/opt/neuro-test"
)

$ErrorActionPreference = "Stop"
if ($HostName -notmatch '^[A-Za-z0-9.-]+$') { throw "HostName contains unsupported characters." }
if ($UserName -notmatch '^[A-Za-z0-9._-]+$') { throw "UserName contains unsupported characters." }
if ($SerialA -notmatch '^[A-Za-z0-9._:-]+$' -or $SerialB -notmatch '^[A-Za-z0-9._:-]+$') {
    throw "Device serials contain unsupported characters."
}
if ($SerialA -eq $SerialB) { throw "SerialA and SerialB must identify different devices." }
if ($PinA -notmatch '^\d{6}$' -or $PinB -notmatch '^\d{6}$') { throw "PinA and PinB must contain exactly six digits." }
if ($RemoteRoot -notmatch '^/[A-Za-z0-9._/-]+$') { throw "RemoteRoot must be an absolute Linux path without spaces." }
foreach ($CommandName in @("ssh", "scp")) {
    if ($null -eq (Get-Command $CommandName -ErrorAction SilentlyContinue)) { throw "Required command is missing: $CommandName" }
}

$ResolvedAppApk = (Resolve-Path -LiteralPath $AppApk).Path
$ResolvedTestApk = (Resolve-Path -LiteralPath $TestApk).Path
$RunId = "{0}-{1}" -f (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ"), ([Guid]::NewGuid().ToString("N").Substring(0, 8))
$Remote = "$UserName@$HostName"
$RemoteStaging = "$RemoteRoot/incoming/$RunId.uploading"
$RemoteReady = "$RemoteRoot/incoming/$RunId"
$EnvironmentLines = @(
    "MODE='$Scenario'"
    "SERIAL_A='$SerialA'"
    "SERIAL_B='$SerialB'"
    "TEST_PIN_A='$PinA'"
    "TEST_PIN_B='$PinB'"
)
$TemporaryEnvironment = Join-Path ([System.IO.Path]::GetTempPath()) "$RunId-job.env"
$EnvironmentContents = ($EnvironmentLines -join "`n") + "`n"
[System.IO.File]::WriteAllText($TemporaryEnvironment, $EnvironmentContents, [System.Text.UTF8Encoding]::new($false))
try {
    & ssh $Remote "umask 077 && mkdir -p '$RemoteStaging'"
    if ($LASTEXITCODE -ne 0) { throw "Could not create remote staging directory." }
    & scp -- $ResolvedAppApk "${Remote}:${RemoteStaging}/app.apk"
    if ($LASTEXITCODE -ne 0) { throw "Could not upload the app APK." }
    & scp -- $ResolvedTestApk "${Remote}:${RemoteStaging}/tests.apk"
    if ($LASTEXITCODE -ne 0) { throw "Could not upload the test APK." }
    & scp -- $TemporaryEnvironment "${Remote}:${RemoteStaging}/job.env"
    if ($LASTEXITCODE -ne 0) { throw "Could not upload the pair configuration." }
    & ssh $Remote "mv '$RemoteStaging' '$RemoteReady' && NEURO_TEST_ROOT='$RemoteRoot' bash '$RemoteRoot/repo/tests/infrastructure/vm/android/runner/enqueue-run.sh' '$RunId'"
    if ($LASTEXITCODE -ne 0) { throw "Could not activate the remote pair job." }
}
finally { Remove-Item -LiteralPath $TemporaryEnvironment -Force -ErrorAction SilentlyContinue }

Write-Output "Run ID: $RunId"
Write-Output "Status: ssh $Remote cat '$RemoteRoot/runs/$RunId/status'"
Write-Output "Results: scp -r ${Remote}:$RemoteRoot/runs/$RunId/results ."
