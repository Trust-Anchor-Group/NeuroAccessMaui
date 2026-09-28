[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $HostName,
    [string] $AppApk,
    [string] $AppApkX86,
    [string] $AppApkX64,
    [Parameter(Mandatory = $true)] [string] $TestApk,
    [string] $UserName = "neuro-test",
    [string] $MatrixPath = (Join-Path $PSScriptRoot "..\config\matrices\smoke\64-bit\device-profiles-x86_64.yaml"),
    [string] $TestClass,
    [string] $TestPhoneNumber,
    [string] $TestPin,
    [string] $TestOtpEndpoint,
    [string] $RemoteRoot = "/opt/neuro-test",
    [switch] $ShowEmulator,
    [switch] $StopOnFailure,
    [ValidateSet("matrix", "cache", "delete")] [string] $StoragePolicy = "matrix",
    [ValidateRange(1, 65535)] [int] $SshPort = 22,
    [string] $IdentityFile,
    [switch] $Wait,
    [string] $ResultsDirectory
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\RemoteClient.Common.ps1"
function Read-TestMatrix {
    param([Parameter(Mandatory = $true)] [string] $Path)
    $Scenarios = [System.Collections.Generic.List[object]]::new()
    $Current = $null
    foreach ($RawLine in [System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path))) {
        $Line = $RawLine.TrimEnd()
        if ([string]::IsNullOrWhiteSpace($Line) -or $Line.TrimStart().StartsWith("#")) { continue }
        if ($Line -match '^\s{2}-\s+name:\s*([A-Za-z0-9._-]+)\s*$') {
            if ($null -ne $Current) { $Scenarios.Add([pscustomobject]$Current) }
            $Current = [ordered]@{ Name = $Matches[1] }; continue
        }
        if ($Line -match '^\s{4}([a-z_]+):\s*(.*?)\s*$' -and $null -ne $Current) {
            $Current[$Matches[1]] = $Matches[2].Trim('"', "'"); continue
        }
        if ($Line -ne 'scenarios:') { throw "Unsupported matrix line: $RawLine" }
    }
    if ($null -ne $Current) { $Scenarios.Add([pscustomobject]$Current) }
    return $Scenarios
}
function Read-BooleanValue {
    param([object] $Value, [bool] $DefaultValue)
    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) { return $DefaultValue }
    if ([string]$Value -eq "true") { return $true }
    if ([string]$Value -eq "false") { return $false }
    throw "Expected true or false, received '$Value'."
}
if (-not [string]::IsNullOrWhiteSpace($TestPhoneNumber) -and $TestPhoneNumber -notmatch '^\d+$') {
    throw "TestPhoneNumber must contain digits only, without a country code."
}
if (-not [string]::IsNullOrWhiteSpace($TestPin) -and $TestPin -notmatch '^\d{6}$') {
    throw "TestPin must contain exactly six digits."
}
if (-not [string]::IsNullOrWhiteSpace($TestOtpEndpoint)) {
    $OtpUri = $null
    if (-not [Uri]::TryCreate($TestOtpEndpoint, [UriKind]::Absolute, [ref]$OtpUri) -or
        $OtpUri.Scheme -notin @("http", "https") -or $TestOtpEndpoint -match '[\[\]\(\)]' -or
        $TestOtpEndpoint.Contains("'") -or
        $TestOtpEndpoint.Contains("`r") -or $TestOtpEndpoint.Contains("`n")) {
        throw "TestOtpEndpoint must be a valid HTTP or HTTPS URL."
    }
}
$Context = New-RemoteClientContext -HostName $HostName -UserName $UserName -RemoteRoot $RemoteRoot `
    -SshPort $SshPort -IdentityFile $IdentityFile
$ResolvedAppApk = if ([string]::IsNullOrWhiteSpace($AppApk)) { $null } else { (Resolve-Path -LiteralPath $AppApk).Path }
$ResolvedAppApkX86 = if ([string]::IsNullOrWhiteSpace($AppApkX86)) { $ResolvedAppApk } else { (Resolve-Path -LiteralPath $AppApkX86).Path }
$ResolvedAppApkX64 = if ([string]::IsNullOrWhiteSpace($AppApkX64)) { $ResolvedAppApk } else { (Resolve-Path -LiteralPath $AppApkX64).Path }
$ResolvedTestApk = (Resolve-Path -LiteralPath $TestApk).Path
$Scenarios = @(Read-TestMatrix -Path $MatrixPath | Where-Object { Read-BooleanValue -Value $_.enabled -DefaultValue $true })
if ($Scenarios.Count -eq 0) { throw "The matrix has no enabled scenarios." }
$RequiredAbis = @($Scenarios | ForEach-Object { if ([string]::IsNullOrWhiteSpace($_.abi)) { "x86_64" } else { [string]$_.abi } } | Sort-Object -Unique)
if ($RequiredAbis -contains "x86" -and [string]::IsNullOrWhiteSpace($ResolvedAppApkX86)) {
    throw "The matrix contains x86 scenarios. Provide -AppApkX86 or a compatible -AppApk."
}
if ($RequiredAbis -contains "x86_64" -and [string]::IsNullOrWhiteSpace($ResolvedAppApkX64)) {
    throw "The matrix contains x86_64 scenarios. Provide -AppApkX64 or a compatible -AppApk."
}
if (($Scenarios | Where-Object { $TestClass -like '*.RegistrationFlowTest' -or $_.test_class -like '*.RegistrationFlowTest' }).Count -gt 0) {
    if ([string]::IsNullOrWhiteSpace($TestPhoneNumber) -or [string]::IsNullOrWhiteSpace($TestPin) -or [string]::IsNullOrWhiteSpace($TestOtpEndpoint)) {
        throw "RegistrationFlowTest requires TestPhoneNumber, TestPin, and TestOtpEndpoint."
    }
}

$RunId = New-RemoteRunId
$TemporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) $RunId
[void](New-Item -ItemType Directory -Path $TemporaryRoot)
$TemporaryEnvironment = Join-Path $TemporaryRoot "job.env"
$TemporaryMatrix = Join-Path $TemporaryRoot "matrix.tsv"
$Lines = [System.Collections.Generic.List[string]]::new()
foreach ($Scenario in $Scenarios) {
    $ApiLevel = [string]$Scenario.api_level; $DeviceProfile = [string]$Scenario.device_profile
    $Image = if ([string]::IsNullOrWhiteSpace($Scenario.system_image)) { "google_apis" } else { [string]$Scenario.system_image }
    $Abi = if ([string]::IsNullOrWhiteSpace($Scenario.abi)) { "x86_64" } else { [string]$Scenario.abi }
    $Visible = $ShowEmulator.IsPresent -or (Read-BooleanValue -Value $Scenario.show_emulator -DefaultValue $false)
    $Policy = if ($StoragePolicy -eq "matrix") { if ([string]::IsNullOrWhiteSpace($Scenario.storage_policy)) { "cache" } else { [string]$Scenario.storage_policy } } else { $StoragePolicy }
    $Class = if ([string]::IsNullOrWhiteSpace($TestClass)) { [string]$Scenario.test_class } else { $TestClass }
    if ($ApiLevel -notmatch '^\d{2}$' -or $DeviceProfile -notmatch '^[A-Za-z0-9._-]+$' -or $Image -notmatch '^[A-Za-z0-9._-]+$' -or $Abi -notmatch '^[A-Za-z0-9._-]+$') { throw "Invalid values in scenario '$($Scenario.Name)'." }
    if ($Policy -notin @("cache", "delete") -or (-not [string]::IsNullOrWhiteSpace($Class) -and $Class -notmatch '^[A-Za-z0-9_.$#,-]+$')) { throw "Invalid policy or test class in '$($Scenario.Name)'." }
    $Lines.Add("$($Scenario.Name)`t$ApiLevel`t$DeviceProfile`t$Image`t$Abi`t$($Visible.ToString().ToLowerInvariant())`t$Policy`t$Class")
}
[System.IO.File]::WriteAllLines($TemporaryMatrix, $Lines, [System.Text.UTF8Encoding]::new($false))
$Environment = [System.Collections.Generic.List[string]]::new()
$Environment.Add("MODE=matrix")
$Environment.Add("STOP_ON_FAILURE=$($StopOnFailure.IsPresent.ToString().ToLowerInvariant())")
if (-not [string]::IsNullOrWhiteSpace($TestPhoneNumber)) { $Environment.Add("TEST_PHONE_NUMBER='$TestPhoneNumber'") }
if (-not [string]::IsNullOrWhiteSpace($TestPin)) { $Environment.Add("TEST_PIN='$TestPin'") }
if (-not [string]::IsNullOrWhiteSpace($TestOtpEndpoint)) { $Environment.Add("TEST_OTP_ENDPOINT='$TestOtpEndpoint'") }
$Environment.Add("REGISTRATION_USERNAME_TIMESTAMP='$((Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss"))'")
[System.IO.File]::WriteAllText($TemporaryEnvironment, ($Environment -join "`n") + "`n", [System.Text.UTF8Encoding]::new($false))

try {
    $Files = [ordered]@{}
    if (-not [string]::IsNullOrWhiteSpace($ResolvedAppApk)) { $Files['app.apk'] = $ResolvedAppApk }
    if (-not [string]::IsNullOrWhiteSpace($ResolvedAppApkX86)) { $Files['app-x86.apk'] = $ResolvedAppApkX86 }
    if (-not [string]::IsNullOrWhiteSpace($ResolvedAppApkX64)) { $Files['app-x86_64.apk'] = $ResolvedAppApkX64 }
    $Files['tests.apk'] = $ResolvedTestApk
    $Files['job.env'] = $TemporaryEnvironment
    $Files['matrix.tsv'] = $TemporaryMatrix
    $Paths = Publish-RemoteTestJob -Context $Context -RunId $RunId -Files $Files
}
finally {
    Remove-Item -LiteralPath $TemporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
}
Complete-RemoteTestSubmission -Context $Context -RunId $RunId -Paths $Paths -Wait:$Wait -ResultsDirectory $ResultsDirectory
