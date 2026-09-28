param(
    [Parameter(Mandatory = $true)][string]$AdbPath,
    [Parameter(Mandatory = $true)][string]$TestApkPath,
    [Parameter(Mandatory = $true)][string]$ReportDirectory,
    [Parameter(Mandatory = $true)][string]$DeviceSerialA,
    [Parameter(Mandatory = $true)][string]$DeviceSerialB
)

. (Join-Path $PSScriptRoot "TestRunner.Common.ps1")
. (Join-Path $PSScriptRoot "ContactsRunner.Common.ps1")

if ($DeviceSerialA -eq $DeviceSerialB) { throw "A and B must be separate Android devices." }
foreach ($Serial in @($DeviceSerialA, $DeviceSerialB)) {
    if ([string]::IsNullOrWhiteSpace($Serial)) { throw "Specify both device serials from adb devices." }
    Get-ConnectedAndroidDeviceSerial -AdbPath $AdbPath -RequestedDeviceSerial $Serial | Out-Null
}
$PinA = [Environment]::GetEnvironmentVariable("NEUROACCESS_TEST_PIN_A")
$PinB = [Environment]::GetEnvironmentVariable("NEUROACCESS_TEST_PIN_B")
if ($PinA -notmatch '^\d{6}$' -or $PinB -notmatch '^\d{6}$') {
    throw "Set NEUROACCESS_TEST_PIN_A and NEUROACCESS_TEST_PIN_B to the current six-digit PINs in .env."
}
$ApplicationPackage = "com.tag.NeuroAccess"

function Read-IdentityResult {
    <# .SYNOPSIS
    Validates exported identities before passing them to Android shell instrumentation.
    #>
    param([hashtable]$Result)
    if (-not $Result.ContainsKey("contactIdentity")) { throw "Device did not export its Neuro-ID." }
    $Identity = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Result.contactIdentity))
    if ($Identity -notmatch '^[a-zA-Z0-9-]+@[a-zA-Z0-9.-]+$') { throw "Unexpected Neuro-ID format." }
    return $Identity
}

$Stopwatch = [Diagnostics.Stopwatch]::StartNew()
$Status = "FAILED"
$Message = ""
try {
    foreach ($Serial in @($DeviceSerialA, $DeviceSerialB)) {
        Initialize-AndroidTestDevice -AdbPath $AdbPath -TestApkPath $TestApkPath -ApplicationPackage $ApplicationPackage -RequestedDeviceSerial $Serial | Out-Null
    }
    $IdentityA = Read-IdentityResult (Invoke-ContactPhase -AdbPath $AdbPath -Role A -Serial $DeviceSerialA -Pin $PinA -Phase identify)
    $IdentityB = Read-IdentityResult (Invoke-ContactPhase -AdbPath $AdbPath -Role B -Serial $DeviceSerialB -Pin $PinB -Phase identify)
    if ($IdentityA -eq $IdentityB) { throw "A and B have the same Neuro-ID. Use two different accounts." }
    Write-Host "PRECHECK passed: two different approved identities read from Show ID."
    $ExportA = Invoke-ContactPhase -AdbPath $AdbPath -Role A -Serial $DeviceSerialA -Pin $PinA -Phase export -Extra @{ ownIdentity = $IdentityA }
    if (-not $ExportA.ContainsKey("contactLink")) { throw "A did not export a QR link." }
    $PhaseJobs = @(
        Start-Job -ScriptBlock {
            param($Scripts, $Adb, $Serial, $Pin)
            . (Join-Path $Scripts "TestRunner.Common.ps1")
            . (Join-Path $Scripts "ContactsRunner.Common.ps1")
            Invoke-ContactPhase -AdbPath $Adb -Role A -Serial $Serial -Pin $Pin -Phase accept
        } -ArgumentList $PSScriptRoot, $AdbPath, $DeviceSerialA, $PinA
        Start-Job -ScriptBlock {
            param($Scripts, $Adb, $Serial, $Pin, $OwnIdentity, $PeerIdentity, $PeerLink)
            . (Join-Path $Scripts "TestRunner.Common.ps1")
            . (Join-Path $Scripts "ContactsRunner.Common.ps1")
            Invoke-ContactPhase -AdbPath $Adb -Role B -Serial $Serial -Pin $Pin -Phase add -Extra @{
                ownIdentity = $OwnIdentity; peerIdentity = $PeerIdentity; peerLink = $PeerLink
            }
        } -ArgumentList $PSScriptRoot, $AdbPath, $DeviceSerialB, $PinB, $IdentityB, $IdentityA, $ExportA.contactLink
    )
    try {
        Wait-Job -Job $PhaseJobs | Out-Null
        foreach ($PhaseJob in $PhaseJobs) {
            $JobOutput = @(Receive-Job -Job $PhaseJob -ErrorAction SilentlyContinue)
            if ($PhaseJob.State -ne "Completed") {
                $Reason = $PhaseJob.ChildJobs[0].JobStateInfo.Reason
                throw "A coordinated contact phase failed on job $($PhaseJob.Id): $Reason $($JobOutput -join ' ')"
            }
            $JobOutput | Out-Null
        }
    } finally {
        Remove-Job -Job $PhaseJobs -Force -ErrorAction SilentlyContinue
    }
    Write-Host "B added A: scanned Neuro-ID matched A; Remove contact verified."
    $ExportB = Invoke-ContactPhase -AdbPath $AdbPath -Role B -Serial $DeviceSerialB -Pin $PinB -Phase export -Extra @{ ownIdentity = $IdentityB }
    if (-not $ExportB.ContainsKey("contactLink")) { throw "B did not export a QR link." }
    $PhaseJobs = @(
        Start-Job -ScriptBlock {
            param($Scripts, $Adb, $Serial, $Pin, $OwnIdentity, $PeerIdentity, $PeerLink)
            . (Join-Path $Scripts "TestRunner.Common.ps1")
            . (Join-Path $Scripts "ContactsRunner.Common.ps1")
            Invoke-ContactPhase -AdbPath $Adb -Role A -Serial $Serial -Pin $Pin -Phase add -Extra @{
                ownIdentity = $OwnIdentity; peerIdentity = $PeerIdentity; peerLink = $PeerLink
            }
        } -ArgumentList $PSScriptRoot, $AdbPath, $DeviceSerialA, $PinA, $IdentityA, $IdentityB, $ExportB.contactLink
        Start-Job -ScriptBlock {
            param($Scripts, $Adb, $Serial, $Pin)
            . (Join-Path $Scripts "TestRunner.Common.ps1")
            . (Join-Path $Scripts "ContactsRunner.Common.ps1")
            Invoke-ContactPhase -AdbPath $Adb -Role B -Serial $Serial -Pin $Pin -Phase accept
        } -ArgumentList $PSScriptRoot, $AdbPath, $DeviceSerialB, $PinB
    )
    try {
        Wait-Job -Job $PhaseJobs | Out-Null
        foreach ($PhaseJob in $PhaseJobs) {
            $JobOutput = @(Receive-Job -Job $PhaseJob -ErrorAction SilentlyContinue)
            if ($PhaseJob.State -ne "Completed") {
                $Reason = $PhaseJob.ChildJobs[0].JobStateInfo.Reason
                throw "A coordinated contact phase failed on job $($PhaseJob.Id): $Reason $($JobOutput -join ' ')"
            }
            $JobOutput | Out-Null
        }
    } finally {
        Remove-Job -Job $PhaseJobs -Force -ErrorAction SilentlyContinue
    }
    Write-Host "A added B: scanned Neuro-ID matched B; Remove contact verified."
    Invoke-ContactPhase -AdbPath $AdbPath -Role A -Serial $DeviceSerialA -Pin $PinA -Phase verifyContact -Extra @{
        ownIdentity = $IdentityA; peerIdentity = $IdentityB
    } | Out-Null
    Invoke-ContactPhase -AdbPath $AdbPath -Role B -Serial $DeviceSerialB -Pin $PinB -Phase verifyContact -Extra @{
        ownIdentity = $IdentityB; peerIdentity = $IdentityA
    } | Out-Null
    Write-Host "CONTACTS PASSED: both saved contacts verified through Apps / Contacts after restarting."
    $ChatRunId = [Guid]::NewGuid().ToString("N")
    Write-Host "CHAT [$ChatRunId]: A to B and B to A, each checked in the receiver's incoming bubbles."
    Invoke-MutualChatExchange -AdbPath $AdbPath -ScriptsDirectory $PSScriptRoot -SerialA $DeviceSerialA -SerialB $DeviceSerialB -PinA $PinA -PinB $PinB -IdentityA $IdentityA -IdentityB $IdentityB -RunId $ChatRunId
    $Status = "PASSED"
    $Message = "Both contacts verified through Apps / Contacts after restart; both unique device messages received. Run: $ChatRunId"
} catch {
    $Message = [regex]::Replace($_.Exception.Message,
        '(?m)^INSTRUMENTATION_STATUS: (?:contact(?:Identity|Link|Added|Verified)|chatVerified)=.*$', '[REDACTED device transfer]')
    Write-Host $Message -ForegroundColor Red
} finally {
    $Stopwatch.Stop()
    foreach ($Serial in @($DeviceSerialA, $DeviceSerialB)) {
        try { Stop-AndroidApplication -AdbPath $AdbPath -DeviceSerial $Serial -ApplicationPackage $ApplicationPackage }
        catch { Write-Warning "Could not stop the test app on $Serial." }
    }
}
$Result = New-AndroidAutomationResult -Group "Contacts" -Name "Mutual contacts (two devices)" -Status $Status -DurationSeconds $Stopwatch.Elapsed.TotalSeconds -Message $Message -Output $Message
Write-AndroidAutomationReports -Results @($Result) -SuiteName "NeuroAccess mutual contacts" -ReportDirectory $ReportDirectory -ReportFileStem "mutual-contacts"
if ($Status -ne "PASSED") { exit 1 }
Write-Host "Mutual contacts PASSED"
