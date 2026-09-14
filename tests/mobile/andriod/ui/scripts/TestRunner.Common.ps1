Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-ConnectedAndroidDeviceSerial {
    param([string]$AdbPath, [string]$RequestedDeviceSerial)

    $DeviceLines = & $AdbPath devices
    if ($LASTEXITCODE -ne 0) { throw "Unable to list connected Android devices." }
    $ConnectedDevices = @(
        $DeviceLines | Select-Object -Skip 1 | ForEach-Object {
            $Columns = $_.Trim() -split "\s+"
            if ($Columns.Count -ge 2 -and $Columns[1] -eq "device") { $Columns[0] }
        }
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedDeviceSerial)) {
        if ($RequestedDeviceSerial -notin $ConnectedDevices) {
            throw "Android device '$RequestedDeviceSerial' is not connected and authorized."
        }
        return $RequestedDeviceSerial
    }
    if ($ConnectedDevices.Count -ne 1) {
        throw "Connect exactly one Android device, or run Gradle with -PdeviceSerial=<serial>."
    }
    return $ConnectedDevices[0]
}

function Protect-AndroidDiagnosticText {
    <#
    .SYNOPSIS
    Removes literal configuration values from text before it reaches logs or reports.
    .PARAMETER Text
    The command output or diagnostic message to sanitize.
    .PARAMETER SensitiveValues
    Values collected from the instrumentation arguments.
    #>
    param([AllowEmptyString()][string]$Text, [string[]]$SensitiveValues = @())

    $Patterns = @(
        $SensitiveValues |
            Where-Object { -not [string]::IsNullOrEmpty($_) } |
            Sort-Object -Unique -CaseSensitive |
            Sort-Object -Property Length -Descending |
            ForEach-Object { [regex]::Escape($_) }
    )
    if ($Patterns.Count -eq 0) {
        return $Text
    }
    return [regex]::Replace($Text, ($Patterns -join "|"), "[REDACTED]")
}

function ConvertTo-AndroidProcessArgument {
    <#
    .SYNOPSIS
    Quotes one Windows process argument, including embedded quotes and trailing backslashes.
    .PARAMETER Value
    The original argument passed to ADB.
    #>
    param([AllowEmptyString()][string]$Value)

    $Escaped = [regex]::Replace($Value, '(\\*)"', '$1$1\"')
    $Escaped = [regex]::Replace($Escaped, '(\\+)$', '$1$1')
    return '"' + $Escaped + '"'
}


function Invoke-AndroidAdbCommand {
    <#
    .SYNOPSIS
    Executes ADB and sanitizes its output before returning it or reporting a failure.
    .PARAMETER AdbPath
    Path to the ADB executable.
    .PARAMETER DeviceSerial
    Serial of the target Android device.
    .PARAMETER AdbArguments
    Original command arguments, passed unchanged to ADB.
    .PARAMETER TimeoutSeconds
    Maximum process and output-drain duration, excluding bounded termination cleanup.
    #>
    param([string]$AdbPath, [string]$DeviceSerial, [string[]]$AdbArguments,
        [ValidateRange(1, 86400)][int]$TimeoutSeconds = 120)

    $SensitiveValues = [System.Collections.Generic.List[string]]::new()
    $DiagnosticArguments = @("class", "languageCode", "personalNumberAgeGroup")
    $TestSelector = ""
    for ($Index = 0; $Index -lt $AdbArguments.Count - 2; $Index++) {
        if ($AdbArguments[$Index] -eq "-e") {
            $ArgumentName = $AdbArguments[$Index + 1]
            $ArgumentValue = $AdbArguments[$Index + 2]
            if ($ArgumentName -notin $DiagnosticArguments) {
                $SensitiveValues.Add($ArgumentValue)
            } elseif ($ArgumentName -eq "class") {
                $TestSelector = $ArgumentValue
            }
            $Index += 2
        }
    }

    $CommandDescription = "ADB command"
    if ($AdbArguments.Count -ge 3 -and
        $AdbArguments[0] -eq "shell" -and
        $AdbArguments[1] -eq "am" -and
        $AdbArguments[2] -eq "instrument") {
        $CommandDescription = "ADB instrumentation"
    }
    $CommandDescription += " on device '$DeviceSerial'"
    if (-not [string]::IsNullOrEmpty($TestSelector)) {
        $CommandDescription += " for test '$TestSelector'"
    }
    $SafeDescription = Protect-AndroidDiagnosticText -Text $CommandDescription -SensitiveValues $SensitiveValues.ToArray()

    $Process = [System.Diagnostics.Process]::new()
    $Process.StartInfo.FileName = $AdbPath
    $Process.StartInfo.UseShellExecute = $false
    $Process.StartInfo.CreateNoWindow = $true
    $Process.StartInfo.RedirectStandardOutput = $true
    $Process.StartInfo.RedirectStandardError = $true
    $Process.StartInfo.Arguments = (@(
        @("-s", $DeviceSerial) + $AdbArguments |
            ForEach-Object { ConvertTo-AndroidProcessArgument -Value $_ }
    ) -join " ")
    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $Started = $false
    $TimedOut = $false
    $CommandExitCode = -1
    $CommandOutput = ""
    try {
        $Started = $Process.Start()
        if (-not $Started) { throw "ADB process did not start." }
        # Drain both pipes concurrently to avoid deadlocks when either buffer fills up.
        $StandardOutput = $Process.StandardOutput.ReadToEndAsync()
        $StandardError = $Process.StandardError.ReadToEndAsync()
        $Remaining = [Math]::Max(0, $TimeoutSeconds * 1000 - [int]$Stopwatch.ElapsedMilliseconds)
        if (-not $Process.WaitForExit($Remaining)) {
            $TimedOut = $true
        } else {
            $CommandExitCode = $Process.ExitCode
            $Remaining = [Math]::Max(0, $TimeoutSeconds * 1000 - [int]$Stopwatch.ElapsedMilliseconds)
            $TimedOut = -not [System.Threading.Tasks.Task]::WaitAll(
                [System.Threading.Tasks.Task[]]@($StandardOutput, $StandardError), $Remaining)
        }
        if ($TimedOut -and -not $Process.HasExited) {
            $Process.Kill()
            $Process.WaitForExit(5000) | Out-Null
        }
        if ($StandardOutput.Status -eq [System.Threading.Tasks.TaskStatus]::RanToCompletion) {
            $CommandOutput += $StandardOutput.Result
        }
        if ($StandardError.Status -eq [System.Threading.Tasks.TaskStatus]::RanToCompletion) {
            $CommandOutput += "`n" + $StandardError.Result
        }
    } catch {
        $SafeFailure = Protect-AndroidDiagnosticText -Text $_.Exception.Message -SensitiveValues $SensitiveValues.ToArray()
        throw "$SafeDescription could not complete.`n$SafeFailure"
    } finally {
        try {
            if ($Started -and -not $Process.HasExited) {
                $Process.Kill()
                $Process.WaitForExit(5000) | Out-Null
            }
        } catch {
            # Do not replace the original failure or expose raw process exception details.
        }
        $Stopwatch.Stop()
        $Process.Dispose()
    }

    $SafeOutput = Protect-AndroidDiagnosticText -Text $CommandOutput -SensitiveValues $SensitiveValues.ToArray()
    if ($TimedOut) {
        throw "$SafeDescription timed out after $TimeoutSeconds seconds; local ADB process stopped. Incomplete output may be omitted.`n$SafeOutput"
    }
    if ($CommandExitCode -ne 0) {
        throw "$SafeDescription failed (exit code $CommandExitCode).`n$SafeOutput"
    }
    return $SafeOutput.Trim()
}

function Initialize-AndroidTestDevice {
    param(
        [string]$AdbPath,
        [string]$TestApkPath,
        [string]$ApplicationPackage,
        [string]$RequestedDeviceSerial
    )

    if (-not (Test-Path -LiteralPath $AdbPath -PathType Leaf)) {
        throw "ADB was not found at '$AdbPath'."
    }
    if (-not (Test-Path -LiteralPath $TestApkPath -PathType Leaf)) {
        throw "The Android test APK was not found at '$TestApkPath'."
    }

    $SelectedDeviceSerial = Get-ConnectedAndroidDeviceSerial -AdbPath $AdbPath -RequestedDeviceSerial $RequestedDeviceSerial
    $InstalledApplicationPath = Invoke-AndroidAdbCommand -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -AdbArguments @("shell", "pm", "path", $ApplicationPackage)
    if ($InstalledApplicationPath -notmatch "^package:") {
        throw "NeuroAccess is not installed on '$SelectedDeviceSerial'. Build and deploy the MAUI application for this device once, then run this task again."
    }

    Write-Host "Running Android tests on $SelectedDeviceSerial"
    Write-Host (Invoke-AndroidAdbCommand -AdbPath $AdbPath -DeviceSerial $SelectedDeviceSerial -AdbArguments @("install", "-r", $TestApkPath))
    return $SelectedDeviceSerial
}

function Clear-AndroidApplicationStorage {
    param([string]$AdbPath, [string]$DeviceSerial, [string]$ApplicationPackage)

    $ClearOutput = Invoke-AndroidAdbCommand -AdbPath $AdbPath -DeviceSerial $DeviceSerial -AdbArguments @("shell", "pm", "clear", $ApplicationPackage)
    if ($ClearOutput -notmatch "Success") {
        throw "Clearing NeuroAccess storage failed: $ClearOutput"
    }
    return $ClearOutput
}

function Stop-AndroidApplication {
    param([string]$AdbPath, [string]$DeviceSerial, [string]$ApplicationPackage)

    Invoke-AndroidAdbCommand -AdbPath $AdbPath -DeviceSerial $DeviceSerial -AdbArguments @("shell", "am", "force-stop", $ApplicationPackage) | Out-Null
}

function Prepare-AndroidApplicationColdStart {
    param(
        [string]$AdbPath,
        [string]$DeviceSerial,
        [string]$ApplicationPackage,
        [int]$SettlingDelaySeconds = 3
    )

    Stop-AndroidApplication -AdbPath $AdbPath -DeviceSerial $DeviceSerial -ApplicationPackage $ApplicationPackage

    $ProcessOutput = Invoke-AndroidAdbCommand -AdbPath $AdbPath -DeviceSerial $DeviceSerial -AdbArguments @("shell", "ps", "-A")
    $EscapedApplicationPackage = [regex]::Escape($ApplicationPackage)
    if ($ProcessOutput -match "(?m)\s$EscapedApplicationPackage(?::\S+)?\s*$") {
        throw "Android application '$ApplicationPackage' was still running after force-stop."
    }

    Invoke-AndroidAdbCommand -AdbPath $AdbPath -DeviceSerial $DeviceSerial -AdbArguments @("shell", "input", "keyevent", "KEYCODE_HOME") | Out-Null
    Start-Sleep -Seconds $SettlingDelaySeconds
}

function Invoke-AndroidInstrumentationTest {
    <#
    .SYNOPSIS
    Runs one instrumentation phase with sanitized diagnostics and a process deadline.
    .PARAMETER AdbPath
    Path to the ADB executable.
    .PARAMETER DeviceSerial
    Serial of the target Android device.
    .PARAMETER TestSelector
    Fully qualified test class and optional method.
    .PARAMETER TestRunner
    Android instrumentation runner component.
    .PARAMETER InstrumentationArguments
    Named configuration values supplied to the test.
    .PARAMETER TimeoutSeconds
    Maximum duration per test phase; defaults to ten minutes.
    #>
    param(
        [string]$AdbPath,
        [string]$DeviceSerial,
        [string]$TestSelector,
        [string]$TestRunner,
        [hashtable]$InstrumentationArguments = @{},
        [ValidateRange(1, 86400)][int]$TimeoutSeconds = 600
    )

    $AdbArguments = [System.Collections.Generic.List[string]]::new()
    @("shell", "am", "instrument", "-w", "-r") | ForEach-Object { $AdbArguments.Add($_) }
    foreach ($ArgumentName in ($InstrumentationArguments.Keys | Sort-Object)) {
        @("-e", $ArgumentName, [string]$InstrumentationArguments[$ArgumentName]) | ForEach-Object { $AdbArguments.Add($_) }
    }
    @("-e", "class", $TestSelector, $TestRunner) | ForEach-Object { $AdbArguments.Add($_) }

    $InstrumentationOutput = Invoke-AndroidAdbCommand -AdbPath $AdbPath -DeviceSerial $DeviceSerial -AdbArguments $AdbArguments.ToArray() -TimeoutSeconds $TimeoutSeconds
    Write-Host $InstrumentationOutput
    if ($InstrumentationOutput -match "FAILURES!!!" -or
        $InstrumentationOutput -match "INSTRUMENTATION_FAILED" -or
        $InstrumentationOutput -notmatch "OK \(1 test\)") {
        throw "Instrumentation test '$TestSelector' failed.`n$InstrumentationOutput"
    }
    return $InstrumentationOutput
}

function New-AndroidAutomationResult {
    param(
        [string]$Group,
        [string]$Name,
        [ValidateSet("PASSED", "FAILED", "SKIPPED")][string]$Status,
        [double]$DurationSeconds = 0,
        [string]$Message = "",
        [string]$Output = ""
    )

    return [PSCustomObject]@{
        Group = $Group
        Name = $Name
        Status = $Status
        DurationSeconds = $DurationSeconds
        Message = $Message
        Output = $Output
    }
}

function Write-AndroidAutomationReports {
    param(
        [object[]]$Results,
        [string]$SuiteName,
        [string]$ReportDirectory,
        [string]$ReportFileStem
    )

    New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null
    $ResultsDirectory = Join-Path (Split-Path $ReportDirectory -Parent) "results"
    New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
    $XmlPath = Join-Path $ResultsDirectory "TEST-$ReportFileStem.xml"
    $FailedResults = @($Results | Where-Object { $_.Status -eq "FAILED" })
    $SkippedResults = @($Results | Where-Object { $_.Status -eq "SKIPPED" })
    $XmlSettings = [System.Xml.XmlWriterSettings]::new()
    $XmlSettings.Indent = $true
    $XmlSettings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $XmlWriter = [System.Xml.XmlWriter]::Create($XmlPath, $XmlSettings)

    try {
        $XmlWriter.WriteStartDocument()
        $XmlWriter.WriteStartElement("testsuite")
        $XmlWriter.WriteAttributeString("name", $SuiteName)
        $XmlWriter.WriteAttributeString("tests", $Results.Count.ToString())
        $XmlWriter.WriteAttributeString("failures", $FailedResults.Count.ToString())
        $XmlWriter.WriteAttributeString("skipped", $SkippedResults.Count.ToString())
        foreach ($Result in $Results) {
            $XmlWriter.WriteStartElement("testcase")
            $XmlWriter.WriteAttributeString("classname", $Result.Group)
            $XmlWriter.WriteAttributeString("name", $Result.Name)
            $XmlWriter.WriteAttributeString("time", $Result.DurationSeconds.ToString("0.000", [System.Globalization.CultureInfo]::InvariantCulture))
            if ($Result.Status -eq "FAILED") {
                $XmlWriter.WriteStartElement("failure")
                $XmlWriter.WriteAttributeString("message", $Result.Message)
                $XmlWriter.WriteString($Result.Output)
                $XmlWriter.WriteEndElement()
            } elseif ($Result.Status -eq "SKIPPED") {
                $XmlWriter.WriteStartElement("skipped")
                $XmlWriter.WriteAttributeString("message", $Result.Message)
                $XmlWriter.WriteEndElement()
            } else {
                $XmlWriter.WriteStartElement("system-out")
                $XmlWriter.WriteString($Result.Output)
                $XmlWriter.WriteEndElement()
            }
            $XmlWriter.WriteEndElement()
        }
        $XmlWriter.WriteEndElement()
        $XmlWriter.WriteEndDocument()
    } finally {
        $XmlWriter.Dispose()
    }

    $HtmlPath = Join-Path $ReportDirectory "index.html"
    $HtmlHead = "<style>body{font-family:Arial,sans-serif;margin:2rem}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ddd;padding:.6rem;text-align:left}th{background:#f3f3f3}</style>"
    $Results | Select-Object Group, Name, Status, DurationSeconds, Message | ConvertTo-Html -Title "$SuiteName report" -Head $HtmlHead | Set-Content -LiteralPath $HtmlPath -Encoding UTF8
    Write-Host "JUnit report: $XmlPath"
    Write-Host "HTML report: $HtmlPath"
}
