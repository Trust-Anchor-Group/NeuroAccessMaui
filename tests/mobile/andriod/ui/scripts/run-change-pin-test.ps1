param(
    [Parameter(Mandatory = $true)][string]$AdbPath,
    [Parameter(Mandatory = $true)][string]$TestApkPath,
    [Parameter(Mandatory = $true)][string]$ReportDirectory,
    [string]$DeviceSerial
)

. (Join-Path $PSScriptRoot "TestRunner.Common.ps1")

$ApplicationPackage = "com.tag.NeuroAccess"
$TestRunner = "com.tag.neuroaccess.neuroaccessespressoautomationtests.test/androidx.test.runner.AndroidJUnitRunner"
$ChangePinTestSelector = "com.tag.neuroaccess.neuroaccessespressoautomationtests.options.ChangePinFlowTest#changePinAndVerifyIdentityBeforeColdStart"
$ColdStartVerificationSelector = "com.tag.neuroaccess.neuroaccessespressoautomationtests.options.ChangePinFlowTest#verifyChangedPinAfterColdStart"

function Get-RequiredPinConfiguration {
    param([string]$EnvironmentVariableName)

    $Value = [Environment]::GetEnvironmentVariable($EnvironmentVariableName)
    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Missing test configuration '$EnvironmentVariableName' for the Change PIN test."
    }
    if ($Value -notmatch "^\d{6}$") {
        throw "$EnvironmentVariableName must contain exactly six digits."
    }
    return $Value
}

$CurrentPin = Get-RequiredPinConfiguration -EnvironmentVariableName "NEUROACCESS_TEST_PIN"
$NewPin = Get-RequiredPinConfiguration -EnvironmentVariableName "NEUROACCESS_TEST_NEW_PIN"
if ($CurrentPin -eq $NewPin) {
    throw "NEUROACCESS_TEST_NEW_PIN must differ from NEUROACCESS_TEST_PIN."
}

$SelectedDeviceSerial = Initialize-AndroidTestDevice -AdbPath $AdbPath -TestApkPath $TestApkPath -ApplicationPackage $ApplicationPackage -RequestedDeviceSerial $DeviceSerial
$Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$Status = "FAILED"
$Message = ""
$Output = ""

try {
    $InstrumentationArguments = @{
        testPin = $CurrentPin
        testNewPin = $NewPin
    }
    $ChangeOutput = Invoke-AndroidInstrumentationTest -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -TestSelector $ChangePinTestSelector -TestRunner $TestRunner -InstrumentationArguments $InstrumentationArguments
    Prepare-AndroidApplicationColdStart -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -ApplicationPackage $ApplicationPackage
    $ColdStartOutput = Invoke-AndroidInstrumentationTest -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -TestSelector $ColdStartVerificationSelector -TestRunner $TestRunner -InstrumentationArguments $InstrumentationArguments
    $Output = "$ChangeOutput`n`n=== Verification after adb force-stop ===`n`n$ColdStartOutput"
    $Status = "PASSED"
} catch {
    $Message = $_.Exception.Message
    $Output = $Message
    Write-Host $Message -ForegroundColor Red
} finally {
    $Stopwatch.Stop()
    try {
        Stop-AndroidApplication -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -ApplicationPackage $ApplicationPackage
    } catch {
        $Output = "$Output`n`nCleanup force-stop failed: $($_.Exception.Message)"
    }
}

$Result = New-AndroidAutomationResult -Group "Options" -Name "Change PIN" -Status $Status -DurationSeconds $Stopwatch.Elapsed.TotalSeconds -Message $Message -Output $Output
Write-AndroidAutomationReports -Results @($Result) -SuiteName "NeuroAccess Change PIN Android test" -ReportDirectory $ReportDirectory -ReportFileStem "change-pin"

if ($Status -eq "FAILED") {
    exit 1
}
Write-Host "Change PIN PASSED"
