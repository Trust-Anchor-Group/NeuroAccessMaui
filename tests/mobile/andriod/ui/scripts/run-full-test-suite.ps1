param(
    [Parameter(Mandatory = $true)][string]$AdbPath,
    [Parameter(Mandatory = $true)][string]$TestApkPath,
    [Parameter(Mandatory = $true)][string]$ReportDirectory,
    [string]$DeviceSerial
)

. (Join-Path $PSScriptRoot "TestRunner.Common.ps1")

$ApplicationPackage = "com.tag.NeuroAccess"
$TestRunner = "com.tag.neuroaccess.neuroaccessespressoautomationtests.test/androidx.test.runner.AndroidJUnitRunner"
$LanguageTestClass = "com.tag.neuroaccess.neuroaccessespressoautomationtests.options.LanguageOptionsFlowTest"
$LanguageCodes = @("en", "sv", "es", "fr", "de", "da", "no", "fi", "sr", "pt", "ro", "ru")
$TestResults = [System.Collections.Generic.List[object]]::new()

function Get-StatefulTestArguments {
    $EnvironmentMappings = [ordered]@{
        testPhoneNumber = "NEUROACCESS_TEST_PHONE_NUMBER"
        testPin = "NEUROACCESS_TEST_PIN"
        testOtpEndpoint = "NEUROACCESS_TEST_OTP_ENDPOINT"
        registrationUsernameTimestamp = "NEUROACCESS_REGISTRATION_USERNAME_TIMESTAMP"
    }
    $InstrumentationArguments = @{}
    foreach ($Entry in $EnvironmentMappings.GetEnumerator()) {
        $Value = [Environment]::GetEnvironmentVariable($Entry.Value)
        if ([string]::IsNullOrWhiteSpace($Value)) {
            throw "Missing test configuration '$($Entry.Value)' for the full suite."
        }
        $InstrumentationArguments[$Entry.Key] = $Value
    }

    if ($InstrumentationArguments.testPin -notmatch "^\d{6}$") {
        throw "NEUROACCESS_TEST_PIN must contain exactly six digits."
    }
    $ConfiguredNewPin = [Environment]::GetEnvironmentVariable("NEUROACCESS_TEST_NEW_PIN")
    if ([string]::IsNullOrWhiteSpace($ConfiguredNewPin)) {
        $ConfiguredNewPin = -join (
            $InstrumentationArguments.testPin.ToCharArray() |
                ForEach-Object { (([int][string]$_ + 1) % 10).ToString() }
        )
    }
    if ($ConfiguredNewPin -notmatch "^\d{6}$") {
        throw "NEUROACCESS_TEST_NEW_PIN must contain exactly six digits when configured."
    }
    if ($ConfiguredNewPin -eq $InstrumentationArguments.testPin) {
        throw "NEUROACCESS_TEST_NEW_PIN must differ from NEUROACCESS_TEST_PIN."
    }
    $InstrumentationArguments.testNewPin = $ConfiguredNewPin

    $OptionalMappings = [ordered]@{
        personalNumberAgeGroup = "NEUROACCESS_TEST_PERSONAL_NUMBER_AGE_GROUP"
        testSocialSecurityNumber = "NEUROACCESS_TEST_SSN"
    }
    foreach ($Entry in $OptionalMappings.GetEnumerator()) {
        $Value = [Environment]::GetEnvironmentVariable($Entry.Value)
        if (-not [string]::IsNullOrWhiteSpace($Value)) {
            $InstrumentationArguments[$Entry.Key] = $Value
        }
    }
    return $InstrumentationArguments
}

function Invoke-ReportedTest {
    param(
        [string]$Group,
        [string]$Name,
        [string]$TestSelector,
        [hashtable]$InstrumentationArguments = @{}
    )

    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $Output = Invoke-AndroidInstrumentationTest -AdbPath $AdbPath -DeviceSerial $script:SelectedDeviceSerial -TestSelector $TestSelector -TestRunner $TestRunner -InstrumentationArguments $InstrumentationArguments
        $Status = "PASSED"
        $Message = ""
    } catch {
        $Output = $_.Exception.Message
        $Status = "FAILED"
        $Message = $_.Exception.Message
        Write-Host $Message -ForegroundColor Red
    } finally {
        $Stopwatch.Stop()
        try {
            Stop-AndroidApplication -AdbPath $AdbPath -DeviceSerial $script:SelectedDeviceSerial -ApplicationPackage $ApplicationPackage
        } catch {
            $Output = "$Output`n`nCleanup force-stop failed: $($_.Exception.Message)"
        }
    }

    $TestResults.Add(
        (New-AndroidAutomationResult -Group $Group -Name $Name -Status $Status -DurationSeconds $Stopwatch.Elapsed.TotalSeconds -Message $Message -Output $Output)
    )
    Write-Host "$Name $Status"
    return $Status -eq "PASSED"
}

function Invoke-ReportedLanguageTest {
    param([string]$LanguageCode)

    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $Outputs = [System.Collections.Generic.List[string]]::new()
    try {
        $ClearOutput = Clear-AndroidApplicationStorage -AdbPath $AdbPath -DeviceSerial $script:SelectedDeviceSerial -ApplicationPackage $ApplicationPackage
        $Outputs.Add("Clear storage: $ClearOutput")
        $LanguageArguments = @{ languageCode = $LanguageCode }
        $Outputs.Add(
            (Invoke-AndroidInstrumentationTest -AdbPath $AdbPath -DeviceSerial $script:SelectedDeviceSerial -TestSelector "$LanguageTestClass#selectLanguageForColdStartVerification" -TestRunner $TestRunner -InstrumentationArguments $LanguageArguments)
        )
        Stop-AndroidApplication -AdbPath $AdbPath -DeviceSerial $script:SelectedDeviceSerial -ApplicationPackage $ApplicationPackage
        $Outputs.Add(
            (Invoke-AndroidInstrumentationTest -AdbPath $AdbPath -DeviceSerial $script:SelectedDeviceSerial -TestSelector "$LanguageTestClass#verifyLanguageAfterColdStart" -TestRunner $TestRunner -InstrumentationArguments $LanguageArguments)
        )
        $Status = "PASSED"
        $Message = ""
    } catch {
        $Status = "FAILED"
        $Message = $_.Exception.Message
        $Outputs.Add($Message)
        Write-Host $Message -ForegroundColor Red
    } finally {
        $Stopwatch.Stop()
        try {
            Stop-AndroidApplication -AdbPath $AdbPath -DeviceSerial $script:SelectedDeviceSerial -ApplicationPackage $ApplicationPackage
        } catch {
            $Outputs.Add("Cleanup force-stop failed: $($_.Exception.Message)")
        }
    }

    $TestResults.Add(
        (New-AndroidAutomationResult -Group "Language options cold start" -Name $LanguageCode -Status $Status -DurationSeconds $Stopwatch.Elapsed.TotalSeconds -Message $Message -Output ($Outputs -join "`n`n"))
    )
    Write-Host "$LanguageCode $Status"
}

function Add-SkippedTest {
    param([string]$Group, [string]$Name, [string]$Reason)

    $TestResults.Add(
        (New-AndroidAutomationResult -Group $Group -Name $Name -Status "SKIPPED" -Message $Reason)
    )
    Write-Host "$Name SKIPPED: $Reason" -ForegroundColor Yellow
}

$StatefulTestArguments = Get-StatefulTestArguments
$script:SelectedDeviceSerial = Initialize-AndroidTestDevice -AdbPath $AdbPath -TestApkPath $TestApkPath -ApplicationPackage $ApplicationPackage -RequestedDeviceSerial $DeviceSerial

foreach ($LanguageCode in $LanguageCodes) {
    Write-Host "`n=== Language cold start: $LanguageCode ==="
    Invoke-ReportedLanguageTest -LanguageCode $LanguageCode
}

Write-Host "`n=== Standalone onboarding smoke test ==="
Clear-AndroidApplicationStorage -AdbPath $AdbPath -DeviceSerial $script:SelectedDeviceSerial -ApplicationPackage $ApplicationPackage | Out-Null
Invoke-ReportedTest -Group "Standalone onboarding" -Name "ID provider navigation" -TestSelector "com.tag.neuroaccess.neuroaccessespressoautomationtests.onboarding.IdProviderTest#selectForMeNavigatesToPhoneVerification" | Out-Null

Write-Host "`n=== Stateful account and identity chain ==="
Clear-AndroidApplicationStorage -AdbPath $AdbPath -DeviceSerial $script:SelectedDeviceSerial -ApplicationPackage $ApplicationPackage | Out-Null
$RegistrationPassed = Invoke-ReportedTest -Group "Account and identity chain" -Name "Registration" -TestSelector "com.tag.neuroaccess.neuroaccessespressoautomationtests.onboarding.RegistrationFlowTest#testUserCompletesRegistrationAndReachesHomePage" -InstrumentationArguments $StatefulTestArguments
if ($RegistrationPassed) {
    $PersonalIdPassed = Invoke-ReportedTest -Group "Account and identity chain" -Name "Personal ID application" -TestSelector "com.tag.neuroaccess.neuroaccessespressoautomationtests.identity.PersonalIdApplicationFlowTest#testUserAppliesForPersonalId" -InstrumentationArguments $StatefulTestArguments
} else {
    Add-SkippedTest -Group "Account and identity chain" -Name "Personal ID application" -Reason "Registration failed, so the required account state was unavailable."
    $PersonalIdPassed = $false
}

if ($PersonalIdPassed) {
    $ChangePinPassed = Invoke-ReportedTest -Group "Account and identity chain" -Name "Change PIN" -TestSelector "com.tag.neuroaccess.neuroaccessespressoautomationtests.options.ChangePinFlowTest#changePinAndVerifyIdentityBeforeColdStart" -InstrumentationArguments $StatefulTestArguments
    if ($ChangePinPassed) {
        Prepare-AndroidApplicationColdStart -AdbPath $AdbPath -DeviceSerial $script:SelectedDeviceSerial -ApplicationPackage $ApplicationPackage
        Invoke-ReportedTest -Group "Account and identity chain" -Name "Change PIN cold-start verification" -TestSelector "com.tag.neuroaccess.neuroaccessespressoautomationtests.options.ChangePinFlowTest#verifyChangedPinAfterColdStart" -InstrumentationArguments $StatefulTestArguments | Out-Null
    } else {
        Add-SkippedTest -Group "Account and identity chain" -Name "Change PIN cold-start verification" -Reason "Changing the PIN failed, so the new PIN could not be verified after force-stop."
    }
} else {
    Add-SkippedTest -Group "Account and identity chain" -Name "Change PIN" -Reason "Personal ID application failed, so the required identity state was unavailable."
    Add-SkippedTest -Group "Account and identity chain" -Name "Change PIN cold-start verification" -Reason "Personal ID application failed, so the required identity state was unavailable."
}

Write-AndroidAutomationReports -Results $TestResults.ToArray() -SuiteName "NeuroAccess full Android test suite" -ReportDirectory $ReportDirectory -ReportFileStem "full-android-test-suite"
$FailedTestCount = @($TestResults | Where-Object { $_.Status -eq "FAILED" }).Count
if ($FailedTestCount -gt 0) {
    Write-Host "$FailedTestCount full-suite test(s) failed." -ForegroundColor Red
    exit 1
}
Write-Host "All $($TestResults.Count) full-suite tests passed."
