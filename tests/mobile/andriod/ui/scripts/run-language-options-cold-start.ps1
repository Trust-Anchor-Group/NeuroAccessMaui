param(
    [Parameter(Mandatory = $true)][string]$AdbPath,
    [Parameter(Mandatory = $true)][string]$TestApkPath,
    [Parameter(Mandatory = $true)][string]$ReportDirectory,
    [string]$DeviceSerial
)

. (Join-Path $PSScriptRoot "TestRunner.Common.ps1")

$ApplicationPackage = "com.tag.NeuroAccess"
$TestRunner = "com.tag.neuroaccess.neuroaccessespressoautomationtests.test/androidx.test.runner.AndroidJUnitRunner"
$TestClass = "com.tag.neuroaccess.neuroaccessespressoautomationtests.options.LanguageOptionsFlowTest"
$LanguageCodes = @("en", "sv", "es", "fr", "de", "da", "no", "fi", "sr", "pt", "ro", "ru")
$SelectedDeviceSerial = Initialize-AndroidTestDevice -AdbPath $AdbPath -TestApkPath $TestApkPath -ApplicationPackage $ApplicationPackage -RequestedDeviceSerial $DeviceSerial
$TestResults = [System.Collections.Generic.List[object]]::new()

foreach ($LanguageCode in $LanguageCodes) {
    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $PhaseOutput = [System.Collections.Generic.List[string]]::new()
    $Status = "PASSED"
    $FailureMessage = ""
    Write-Host "`n=== Language: $LanguageCode ==="

    try {
        $ClearOutput = Clear-AndroidApplicationStorage -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -ApplicationPackage $ApplicationPackage
        $PhaseOutput.Add("Clear storage: $ClearOutput")
        $LanguageArguments = @{ languageCode = $LanguageCode }
        $PhaseOutput.Add(
            (Invoke-AndroidInstrumentationTest -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -TestSelector "$TestClass#selectLanguageForColdStartVerification" -TestRunner $TestRunner -InstrumentationArguments $LanguageArguments)
        )
        Stop-AndroidApplication -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -ApplicationPackage $ApplicationPackage
        $PhaseOutput.Add(
            (Invoke-AndroidInstrumentationTest -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -TestSelector "$TestClass#verifyLanguageAfterColdStart" -TestRunner $TestRunner -InstrumentationArguments $LanguageArguments)
        )
    } catch {
        $Status = "FAILED"
        $FailureMessage = $_.Exception.Message
        $PhaseOutput.Add($FailureMessage)
        Write-Host $FailureMessage -ForegroundColor Red
    } finally {
        try {
            Stop-AndroidApplication -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -ApplicationPackage $ApplicationPackage
        } catch {
            $PhaseOutput.Add("Cleanup force-stop failed: $($_.Exception.Message)")
        }
    }

    $Stopwatch.Stop()
    $TestResults.Add(
        (New-AndroidAutomationResult -Group "Language options cold start" -Name $LanguageCode -Status $Status -DurationSeconds $Stopwatch.Elapsed.TotalSeconds -Message $FailureMessage -Output ($PhaseOutput -join "`n`n"))
    )
    Write-Host "$LanguageCode $Status"
}

Write-AndroidAutomationReports -Results $TestResults.ToArray() -SuiteName "Language options cold start" -ReportDirectory $ReportDirectory -ReportFileStem "language-options-cold-start"
$FailedLanguageCount = @($TestResults | Where-Object { $_.Status -eq "FAILED" }).Count
if ($FailedLanguageCount -gt 0) {
    Write-Host "$FailedLanguageCount language cold-start test(s) failed." -ForegroundColor Red
    exit 1
}
Write-Host "All $($TestResults.Count) language cold-start tests passed."
