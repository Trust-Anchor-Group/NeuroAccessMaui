[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $HostName,
    [Parameter(Mandatory = $true)] [string] $AppApk,
    [Parameter(Mandatory = $true)] [string] $TestApk,
    [Parameter(Mandatory = $true)] [string] $Serial,
    [string] $UserName = "neuro-test",
    [string] $TestClass,
    [string] $TestPhoneNumber,
    [string] $TestPin,
    [string] $TestOtpEndpoint,
    [string] $RemoteRoot = "/opt/neuro-test"
)

$ErrorActionPreference = "Stop"
if ($HostName -notmatch '^[A-Za-z0-9.-]+$') { throw "HostName contains unsupported characters." }
if ($UserName -notmatch '^[A-Za-z0-9._-]+$') { throw "UserName contains unsupported characters." }
if ($Serial -notmatch '^[A-Za-z0-9._:-]+$') { throw "Serial contains unsupported characters." }
if ($RemoteRoot -notmatch '^/[A-Za-z0-9._/-]+$') { throw "RemoteRoot must be an absolute Linux path without spaces." }
if (-not [string]::IsNullOrWhiteSpace($TestClass) -and $TestClass -notmatch '^[A-Za-z0-9_.$#,-]+$') {
    throw "TestClass contains unsupported characters."
}
if (-not [string]::IsNullOrWhiteSpace($TestPhoneNumber) -and $TestPhoneNumber -notmatch '^\d+$') {
    throw "TestPhoneNumber must contain digits only, without a country code."
}
if (-not [string]::IsNullOrWhiteSpace($TestPin) -and $TestPin -notmatch '^\d{6}$') {
    throw "TestPin must contain exactly six digits."
}
if (-not [string]::IsNullOrWhiteSpace($TestOtpEndpoint)) {
    $OtpUri = $null
    if (-not [Uri]::TryCreate($TestOtpEndpoint, [UriKind]::Absolute, [ref] $OtpUri) -or
        $OtpUri.Scheme -notin @("http", "https") -or $TestOtpEndpoint.Contains("'") -or
        $TestOtpEndpoint.Contains("`r") -or $TestOtpEndpoint.Contains("`n")) {
        throw "TestOtpEndpoint must be a valid HTTP or HTTPS URL."
    }
}
if ($TestClass -like '*.onboarding.RegistrationFlowTest') {
    if ([string]::IsNullOrWhiteSpace($TestPhoneNumber) -or
        [string]::IsNullOrWhiteSpace($TestPin) -or
        [string]::IsNullOrWhiteSpace($TestOtpEndpoint)) {
        throw "RegistrationFlowTest requires TestPhoneNumber, TestPin, and TestOtpEndpoint."
    }
}
foreach ($CommandName in @("ssh", "scp")) {
    if ($null -eq (Get-Command $CommandName -ErrorAction SilentlyContinue)) { throw "Required command is missing: $CommandName" }
}
$ResolvedAppApk = (Resolve-Path -LiteralPath $AppApk).Path
$ResolvedTestApk = (Resolve-Path -LiteralPath $TestApk).Path
$RunId = "{0}-{1}" -f (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ"), ([Guid]::NewGuid().ToString("N").Substring(0, 8))
$Remote = "$UserName@$HostName"
$RemoteStaging = "$RemoteRoot/incoming/$RunId.uploading"
$RemoteReady = "$RemoteRoot/incoming/$RunId"
$EnvironmentLines = [System.Collections.Generic.List[string]]::new()
$EnvironmentLines.Add("MODE=single")
$EnvironmentLines.Add("SERIAL='$Serial'")
if (-not [string]::IsNullOrWhiteSpace($TestClass)) { $EnvironmentLines.Add("TEST_CLASS='$TestClass'") }
if (-not [string]::IsNullOrWhiteSpace($TestPhoneNumber)) { $EnvironmentLines.Add("TEST_PHONE_NUMBER='$TestPhoneNumber'") }
if (-not [string]::IsNullOrWhiteSpace($TestPin)) { $EnvironmentLines.Add("TEST_PIN='$TestPin'") }
if (-not [string]::IsNullOrWhiteSpace($TestOtpEndpoint)) { $EnvironmentLines.Add("TEST_OTP_ENDPOINT='$TestOtpEndpoint'") }
$EnvironmentLines.Add("REGISTRATION_USERNAME_TIMESTAMP='$((Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss"))'")
$TemporaryEnvironment = Join-Path ([System.IO.Path]::GetTempPath()) "$RunId-job.env"
$EnvironmentContents = ($EnvironmentLines -join "`n") + "`n"
[System.IO.File]::WriteAllText($TemporaryEnvironment, $EnvironmentContents, [System.Text.UTF8Encoding]::new($false))
try {
    & ssh $Remote "mkdir -p '$RemoteStaging'"
    if ($LASTEXITCODE -ne 0) { throw "Could not create remote staging directory." }
    & scp -- $ResolvedAppApk "${Remote}:${RemoteStaging}/app.apk"
    if ($LASTEXITCODE -ne 0) { throw "Could not upload the app APK." }
    & scp -- $ResolvedTestApk "${Remote}:${RemoteStaging}/tests.apk"
    if ($LASTEXITCODE -ne 0) { throw "Could not upload the test APK." }
    & scp -- $TemporaryEnvironment "${Remote}:${RemoteStaging}/job.env"
    if ($LASTEXITCODE -ne 0) { throw "Could not upload the job configuration." }
    & ssh $Remote "mv '$RemoteStaging' '$RemoteReady' && NEURO_TEST_ROOT='$RemoteRoot' bash '$RemoteRoot/repo/tests/infrastructure/vm/android/runner/enqueue-run.sh' '$RunId'"
    if ($LASTEXITCODE -ne 0) { throw "Could not activate the remote test job." }
}
finally { Remove-Item -LiteralPath $TemporaryEnvironment -Force -ErrorAction SilentlyContinue }
Write-Output "Run ID: $RunId"
Write-Output "Status: ssh $Remote cat '$RemoteRoot/runs/$RunId/status'"
Write-Output "Results: scp -r ${Remote}:$RemoteRoot/runs/$RunId/results ."
