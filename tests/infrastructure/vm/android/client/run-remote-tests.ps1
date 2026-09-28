[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $HostName,
    [Parameter(Mandatory = $true)] [string] $AppApk,
    [Parameter(Mandatory = $true)] [string] $TestApk,
    [string] $Serial,
    [int] $ApiLevel,
    [string] $DeviceProfile = "pixel_6",
    [string] $SystemImage = "google_apis",
    [string] $SystemImageAbi = "x86_64",
    [string] $AvdName,
    [switch] $ShowEmulator,
    [ValidateSet("cache", "delete")] [string] $StoragePolicy = "cache",
    [string] $UserName = "neuro-test",
    [string] $TestClass,
    [string] $TestPhoneNumber,
    [string] $TestPin,
    [string] $TestOtpEndpoint,
    [string] $RemoteRoot = "/opt/neuro-test",
    [ValidateRange(1, 65535)] [int] $SshPort = 22,
    [string] $IdentityFile,
    [switch] $Wait,
    [string] $ResultsDirectory
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\RemoteClient.Common.ps1"

if ([string]::IsNullOrWhiteSpace($Serial) -eq ($ApiLevel -eq 0)) {
    throw "Specify either Serial for an existing device or ApiLevel for a managed emulator."
}
if (-not [string]::IsNullOrWhiteSpace($Serial) -and $Serial -notmatch '^[A-Za-z0-9._:-]+$') {
    throw "Serial contains unsupported characters."
}
if ($ApiLevel -ne 0 -and ($ApiLevel -lt 21 -or $ApiLevel -gt 99)) { throw "ApiLevel is outside the supported range." }
foreach ($Value in @($DeviceProfile, $SystemImage, $SystemImageAbi, $AvdName)) {
    if (-not [string]::IsNullOrWhiteSpace($Value) -and $Value -notmatch '^[A-Za-z0-9._-]+$') {
        throw "Managed emulator values may only contain letters, digits, dots, underscores, and hyphens."
    }
}
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
        $OtpUri.Scheme -notin @("http", "https") -or $TestOtpEndpoint -match '[\[\]\(\)]' -or
        $TestOtpEndpoint.Contains("'") -or $TestOtpEndpoint.Contains("`r") -or $TestOtpEndpoint.Contains("`n")) {
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

$Context = New-RemoteClientContext -HostName $HostName -UserName $UserName -RemoteRoot $RemoteRoot `
    -SshPort $SshPort -IdentityFile $IdentityFile
$ResolvedAppApk = (Resolve-Path -LiteralPath $AppApk).Path
$ResolvedTestApk = (Resolve-Path -LiteralPath $TestApk).Path
$RunId = New-RemoteRunId
$EnvironmentLines = [System.Collections.Generic.List[string]]::new()
$EnvironmentLines.Add("MODE=single")
if ($ApiLevel -ne 0) {
    $EnvironmentLines.Add("DEVICE_MODE=managed")
    $EnvironmentLines.Add("API_LEVEL='$ApiLevel'")
    $EnvironmentLines.Add("DEVICE_PROFILE='$DeviceProfile'")
    $EnvironmentLines.Add("SYSTEM_IMAGE='$SystemImage'")
    $EnvironmentLines.Add("SYSTEM_IMAGE_ABI='$SystemImageAbi'")
    if (-not [string]::IsNullOrWhiteSpace($AvdName)) { $EnvironmentLines.Add("AVD_NAME='$AvdName'") }
    $EnvironmentLines.Add("SHOW_EMULATOR=$($ShowEmulator.IsPresent.ToString().ToLowerInvariant())")
    $EnvironmentLines.Add("STORAGE_POLICY='$StoragePolicy'")
}
else {
    $EnvironmentLines.Add("DEVICE_MODE=existing")
    $EnvironmentLines.Add("SERIAL='$Serial'")
}
if (-not [string]::IsNullOrWhiteSpace($TestClass)) { $EnvironmentLines.Add("TEST_CLASS='$TestClass'") }
if (-not [string]::IsNullOrWhiteSpace($TestPhoneNumber)) { $EnvironmentLines.Add("TEST_PHONE_NUMBER='$TestPhoneNumber'") }
if (-not [string]::IsNullOrWhiteSpace($TestPin)) { $EnvironmentLines.Add("TEST_PIN='$TestPin'") }
if (-not [string]::IsNullOrWhiteSpace($TestOtpEndpoint)) { $EnvironmentLines.Add("TEST_OTP_ENDPOINT='$TestOtpEndpoint'") }
$EnvironmentLines.Add("REGISTRATION_USERNAME_TIMESTAMP='$((Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss"))'")
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