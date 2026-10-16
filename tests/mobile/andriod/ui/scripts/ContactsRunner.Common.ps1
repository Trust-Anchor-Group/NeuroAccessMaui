function Invoke-ContactPhase {
    <# .SYNOPSIS
    Runs one device phase and validates its result without exposing clipboard payloads.
    #>
    param([string]$AdbPath, [string]$Role, [string]$Serial, [string]$Pin,
        [string]$Phase, [hashtable]$Extra = @{})
    $ApplicationPackage = "com.tag.NeuroAccess"
    $TestRunner = "com.tag.neuroaccess.neuroaccessespressoautomationtests.test/androidx.test.runner.AndroidJUnitRunner"
    $TestSelector = "com.tag.neuroaccess.neuroaccessespressoautomationtests.contacts.MutualContactsFlowTest#runDevicePhase"
    Write-Host "[$Role / $Serial] $Phase"
    Stop-AndroidApplication -AdbPath $AdbPath -DeviceSerial $Serial -ApplicationPackage $ApplicationPackage
    $Arguments = @("shell", "am", "instrument", "-w", "-r",
        "-e", "class", $TestSelector, "-e", "contactsCoordinator", "true",
        "-e", "contactsPhase", $Phase, "-e", "testPin", $Pin)
    foreach ($Name in $Extra.Keys) { $Arguments += @("-e", $Name, [string]$Extra[$Name]) }
    $Arguments += $TestRunner
    $Output = Invoke-AndroidAdbCommand -AdbPath $AdbPath -DeviceSerial $Serial -AdbArguments $Arguments -TimeoutSeconds 300
    if ($Output -notmatch 'OK \(1 test\)' -or $Output -match 'FAILURES!!!|INSTRUMENTATION_FAILED|INSTRUMENTATION_ABORTED') {
        throw "[$Role] $Phase failed. Check MutualContactsTest and TestRunner logcat on this device."
    }
    $Values = @{}
    foreach ($Key in @("contactIdentity", "contactLink", "contactAdded", "contactVerified", "chatVerified")) {
        $Found = [regex]::Matches($Output, "(?m)^INSTRUMENTATION_STATUS: $Key=([A-Za-z0-9+/=]+)\r?$")
        if ($Found.Count -gt 1) { throw "Unexpected duplicate result for $Key." }
        if ($Found.Count -eq 1) { $Values[$Key] = $Found[0].Groups[1].Value }
    }
    $ConfirmationKey = switch ($Phase) {
        "add" { "contactAdded" }
        "verifyContact" { "contactVerified" }
        "chatInitiator" { "chatVerified" }
        "chatResponder" { "chatVerified" }
        default { "" }
    }
    if ($ConfirmationKey) {
        $ExpectedValue = if ($ConfirmationKey -eq "chatVerified") { $Extra.chatRunId } else { $Extra.peerIdentity }
        $Expected = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes([string]$ExpectedValue))
        if (-not $Values.ContainsKey($ConfirmationKey) -or $Values[$ConfirmationKey] -cne $Expected) {
            throw "[$Role] Missing or incorrect $Phase confirmation from the device."
        }
    }
    Write-Host "[$Role] $Phase passed"
    return $Values
}

function Invoke-MutualChatExchange {
    <# .SYNOPSIS
    Keeps both test activities alive while exchanging two unique messages and confirming receipt.
    #>
    param([string]$AdbPath, [string]$ScriptsDirectory, [string]$SerialA, [string]$SerialB,
        [string]$PinA, [string]$PinB, [string]$IdentityA, [string]$IdentityB, [string]$RunId)
    if ($RunId -notmatch '^[a-f0-9]{32}$') { throw "Invalid chat run identifier." }
    $Jobs = @()
    $Worker = {
        param($Scripts, $Adb, $Serial, $Pin, $Role, $OwnIdentity, $PeerIdentity, $Run)
        . (Join-Path $Scripts "TestRunner.Common.ps1")
        . (Join-Path $Scripts "ContactsRunner.Common.ps1")
        $Phase = if ($Role -eq "A") { "chatInitiator" } else { "chatResponder" }
        $Result = Invoke-ContactPhase -AdbPath $Adb -Role $Role -Serial $Serial -Pin $Pin -Phase $Phase -Extra @{
            ownIdentity = $OwnIdentity; peerIdentity = $PeerIdentity; chatRunId = $Run
        }
        [pscustomobject]@{ Role = $Role; ChatRunId = $Run; Confirmation = $Result.chatVerified }
    }
    try {
        $Jobs += Start-Job -ScriptBlock $Worker -ArgumentList $ScriptsDirectory, $AdbPath, $SerialB, $PinB, "B", $IdentityB, $IdentityA, $RunId
        $Jobs += Start-Job -ScriptBlock $Worker -ArgumentList $ScriptsDirectory, $AdbPath, $SerialA, $PinA, "A", $IdentityA, $IdentityB, $RunId
        $Pending = @($Jobs)
        $Deadline = [DateTime]::UtcNow.AddSeconds(330)
        $VerifiedRoles = @()
        $Expected = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($RunId))
        while ($Pending.Count -gt 0) {
            if ([DateTime]::UtcNow -ge $Deadline) { throw "Timed out waiting for the two-device chat exchange." }
            $Finished = $Pending | Wait-Job -Any -Timeout 5
            if ($null -eq $Finished) { continue }
            $Output = @($Finished | Receive-Job -ErrorAction Stop)
            if ($Finished.State -ne "Completed") { throw "A chat worker did not complete successfully." }
            $Confirmations = @($Output | Where-Object { $null -ne $_.PSObject.Properties["ChatRunId"] })
            if ($Confirmations.Count -ne 1 -or $Confirmations[0].ChatRunId -cne $RunId -or
                $Confirmations[0].Confirmation -cne $Expected -or $Confirmations[0].Role -notin @("A", "B")) {
                throw "A chat worker did not confirm the current run."
            }
            if ($Confirmations[0].Role -eq "A") {
                # Release B only after A's incoming-bubble assertion has passed.
                Invoke-AndroidAdbCommand -AdbPath $AdbPath -DeviceSerial $SerialB -AdbArguments @(
                    "shell", "run-as", "com.tag.NeuroAccess", "touch", "cache/mutual-chat-$RunId.release"
                ) -TimeoutSeconds 15 | Out-Null
            }
            $VerifiedRoles += $Confirmations[0].Role
            $Pending = @($Pending | Where-Object { $_.Id -ne $Finished.Id })
        }
        if (@($VerifiedRoles | Sort-Object -Unique).Count -ne 2) { throw "Both devices must confirm receipt." }
        Write-Host "CHAT PASSED [$RunId]: A received B's message; B received A's message."
    } finally {
        # Stop only this exchange's jobs and selected devices, including on an early worker failure.
        if (@($Jobs | Where-Object { $_.State -eq "Running" }).Count -gt 0) {
            foreach ($Serial in @($SerialA, $SerialB)) {
                try { Stop-AndroidApplication -AdbPath $AdbPath -DeviceSerial $Serial -ApplicationPackage "com.tag.NeuroAccess" }
                catch { Write-Warning "Could not stop the chat activity on $Serial." }
            }
        }
        foreach ($Job in $Jobs) {
            if ($Job.State -eq "Running") { $Job | Stop-Job }
            $Job | Remove-Job -Force
        }
    }
}
