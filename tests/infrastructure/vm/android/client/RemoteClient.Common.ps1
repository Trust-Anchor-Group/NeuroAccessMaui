Set-StrictMode -Version Latest

function Assert-RequiredCommand {
    param([Parameter(Mandatory = $true)] [string] $Name)

    if ($null -eq (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required client command is missing: $Name"
    }
}

function Join-RemotePath {
    param(
        [Parameter(Mandatory = $true)] [string] $BasePath,
        [Parameter(Mandatory = $true)] [string] $ChildPath
    )

    return $BasePath.TrimEnd('/') + '/' + $ChildPath.TrimStart('/')
}

function New-RemoteClientContext {
    param(
        [Parameter(Mandatory = $true)] [string] $HostName,
        [Parameter(Mandatory = $true)] [string] $UserName,
        [Parameter(Mandatory = $true)] [string] $RemoteRoot,
        [ValidateRange(1, 65535)] [int] $SshPort = 22,
        [string] $IdentityFile
    )

    if ($HostName -notmatch '^[A-Za-z0-9.-]+$') { throw "HostName contains unsupported characters." }
    if ($UserName -notmatch '^[A-Za-z0-9._-]+$') { throw "UserName contains unsupported characters." }
    if ($RemoteRoot -notmatch '^/[A-Za-z0-9._/-]+$') { throw "RemoteRoot must be an absolute Linux path without spaces." }

    Assert-RequiredCommand -Name 'ssh'
    Assert-RequiredCommand -Name 'scp'

    $ResolvedIdentityFile = $null
    if (-not [string]::IsNullOrWhiteSpace($IdentityFile)) {
        $ResolvedIdentityFile = (Resolve-Path -LiteralPath $IdentityFile).Path
    }

    [pscustomobject]@{
        HostName = $HostName
        UserName = $UserName
        Destination = "$UserName@$HostName"
        SshPort = $SshPort
        IdentityFile = $ResolvedIdentityFile
        RemoteRoot = $RemoteRoot.TrimEnd('/')
    }
}

function Get-SshOptions {
    param([Parameter(Mandatory = $true)] [psobject] $Context)

    $Options = [System.Collections.Generic.List[string]]::new()
    $Options.Add('-p')
    $Options.Add([string]$Context.SshPort)
    if (-not [string]::IsNullOrWhiteSpace($Context.IdentityFile)) {
        $Options.Add('-i')
        $Options.Add([string]$Context.IdentityFile)
    }
    return $Options.ToArray()
}

function Get-ScpOptions {
    param([Parameter(Mandatory = $true)] [psobject] $Context)

    $Options = [System.Collections.Generic.List[string]]::new()
    $Options.Add('-P')
    $Options.Add([string]$Context.SshPort)
    if (-not [string]::IsNullOrWhiteSpace($Context.IdentityFile)) {
        $Options.Add('-i')
        $Options.Add([string]$Context.IdentityFile)
    }
    return $Options.ToArray()
}

function Invoke-RemoteCommand {
    param(
        [Parameter(Mandatory = $true)] [psobject] $Context,
        [Parameter(Mandatory = $true)] [string] $Command,
        [Parameter(Mandatory = $true)] [string] $Operation,
        [switch] $CaptureOutput,
        [switch] $Quiet
    )

    if (-not $Quiet.IsPresent) { Write-Host "[SSH] $Operation" }
    $Arguments = [System.Collections.Generic.List[string]]::new()
    foreach ($Option in (Get-SshOptions -Context $Context)) { $Arguments.Add($Option) }
    $Arguments.Add([string]$Context.Destination)
    $Arguments.Add($Command)

    if ($CaptureOutput.IsPresent) {
        $Output = @(& ssh @Arguments 2>&1)
        $ExitCode = $LASTEXITCODE
        if ($ExitCode -ne 0) {
            $Details = ($Output | ForEach-Object { "$_" }) -join [Environment]::NewLine
            throw "$Operation failed over SSH with exit code $ExitCode.$([Environment]::NewLine)$Details"
        }
        return $Output
    }

    $Output = @(& ssh @Arguments 2>&1)
    $ExitCode = $LASTEXITCODE
    foreach ($Line in $Output) { Write-Host "$Line" }
    if ($ExitCode -ne 0) { throw "$Operation failed over SSH with exit code $ExitCode." }
}

function Send-RemoteFile {
    param(
        [Parameter(Mandatory = $true)] [psobject] $Context,
        [Parameter(Mandatory = $true)] [string] $LocalPath,
        [Parameter(Mandatory = $true)] [string] $RemotePath,
        [Parameter(Mandatory = $true)] [string] $Description
    )

    $ResolvedLocalPath = (Resolve-Path -LiteralPath $LocalPath).Path
    Write-Host "[CLIENT] Uploading $Description"
    $Arguments = [System.Collections.Generic.List[string]]::new()
    foreach ($Option in (Get-ScpOptions -Context $Context)) { $Arguments.Add($Option) }
    $Arguments.Add('--')
    $Arguments.Add($ResolvedLocalPath)
    $Arguments.Add("$($Context.Destination):$RemotePath")
    $Output = @(& scp @Arguments 2>&1)
    $ExitCode = $LASTEXITCODE
    foreach ($Line in $Output) { Write-Host "$Line" }
    if ($ExitCode -ne 0) { throw "Upload failed for $Description with exit code $ExitCode." }
}

function Receive-RemoteDirectory {
    param(
        [Parameter(Mandatory = $true)] [psobject] $Context,
        [Parameter(Mandatory = $true)] [string] $RemotePath,
        [Parameter(Mandatory = $true)] [string] $LocalPath
    )

    [void](New-Item -ItemType Directory -Path $LocalPath -Force)
    $ResolvedLocalPath = (Resolve-Path -LiteralPath $LocalPath).Path
    Write-Host "[RESULT] Downloading test results to $ResolvedLocalPath"
    $Arguments = [System.Collections.Generic.List[string]]::new()
    foreach ($Option in (Get-ScpOptions -Context $Context)) { $Arguments.Add($Option) }
    $Arguments.Add('-r')
    $Arguments.Add('--')
    $Arguments.Add("$($Context.Destination):$RemotePath/.")
    $Arguments.Add($ResolvedLocalPath)
    $Output = @(& scp @Arguments 2>&1)
    $ExitCode = $LASTEXITCODE
    foreach ($Line in $Output) { Write-Host "$Line" }
    if ($ExitCode -ne 0) { throw "Result download failed with exit code $ExitCode." }
}

function New-RemoteRunId {
    return "{0}-{1}" -f (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
}

function Get-RemoteRunPaths {
    param(
        [Parameter(Mandatory = $true)] [psobject] $Context,
        [Parameter(Mandatory = $true)] [string] $RunId
    )

    if ($RunId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$') { throw "RunId contains unsupported characters." }
    [pscustomobject]@{
        Staging = Join-RemotePath -BasePath $Context.RemoteRoot -ChildPath "incoming/$RunId.uploading"
        Ready = Join-RemotePath -BasePath $Context.RemoteRoot -ChildPath "incoming/$RunId"
        Run = Join-RemotePath -BasePath $Context.RemoteRoot -ChildPath "runs/$RunId"
        EnqueueScript = Join-RemotePath -BasePath $Context.RemoteRoot -ChildPath 'repo/tests/infrastructure/vm/android/runner/enqueue-run.sh'
    }
}

function Publish-RemoteTestJob {
    param(
        [Parameter(Mandatory = $true)] [psobject] $Context,
        [Parameter(Mandatory = $true)] [string] $RunId,
        [Parameter(Mandatory = $true)] [System.Collections.IDictionary] $Files
    )

    $Paths = Get-RemoteRunPaths -Context $Context -RunId $RunId
    Invoke-RemoteCommand -Context $Context -Operation "Preparing remote run $RunId" -Command "umask 077 && mkdir -p -- '$($Paths.Staging)'"
    try {
        foreach ($Entry in $Files.GetEnumerator()) {
            $RemoteName = [string]$Entry.Key
            if ($RemoteName -notmatch '^[A-Za-z0-9._-]+$') { throw "Remote upload filename is invalid: $RemoteName" }
            Send-RemoteFile -Context $Context -LocalPath ([string]$Entry.Value) `
                -RemotePath (Join-RemotePath -BasePath $Paths.Staging -ChildPath $RemoteName) -Description $RemoteName
        }
        $ActivateCommand = "mv -- '$($Paths.Staging)' '$($Paths.Ready)' && NEURO_TEST_ROOT='$($Context.RemoteRoot)' bash '$($Paths.EnqueueScript)' '$RunId'"
        Invoke-RemoteCommand -Context $Context -Operation "Activating remote run $RunId" -Command $ActivateCommand
    }
    catch {
        $PublishError = $_
        try {
            Invoke-RemoteCommand -Context $Context -Operation "Cleaning failed upload for $RunId" `
                -Command "rm -rf -- '$($Paths.Staging)' '$($Paths.Ready)'" -Quiet | Out-Null
        }
        catch {
            Write-Warning "Remote staging cleanup also failed for run $RunId."
        }
        throw $PublishError
    }

    return $Paths
}

function Wait-RemoteTestJob {
    param(
        [Parameter(Mandatory = $true)] [psobject] $Context,
        [Parameter(Mandatory = $true)] [string] $RunId,
        [ValidateRange(1, 3600)] [int] $PollIntervalSeconds = 5
    )

    $Paths = Get-RemoteRunPaths -Context $Context -RunId $RunId
    Write-Host "[CLIENT] Waiting for remote run $RunId"
    while ($true) {
        $StatusCommand = "if [ -f '$($Paths.Run)/status' ]; then cat '$($Paths.Run)/status'; elif [ -d '$($Context.RemoteRoot)/queue/$RunId' ]; then printf 'queued\n'; else printf 'missing\n'; fi"
        $Output = Invoke-RemoteCommand -Context $Context -Operation "Reading status for $RunId" `
            -Command $StatusCommand -CaptureOutput -Quiet
        $Status = "$($Output | Select-Object -Last 1)".Trim()
        if ($Status -in @('passed', 'failed')) { return $Status }
        if ($Status -notin @('queued', 'running')) { throw "Remote run $RunId returned unexpected status '$Status'." }
        Start-Sleep -Seconds $PollIntervalSeconds
    }
}

function Complete-RemoteTestSubmission {
    param(
        [Parameter(Mandatory = $true)] [psobject] $Context,
        [Parameter(Mandatory = $true)] [string] $RunId,
        [Parameter(Mandatory = $true)] [psobject] $Paths,
        [switch] $Wait,
        [string] $ResultsDirectory
    )

    Write-Output "Run ID: $RunId"
    Write-Output "Status: ssh -p $($Context.SshPort) $($Context.Destination) cat '$($Paths.Run)/status'"
    Write-Output "Detailed log: ssh -p $($Context.SshPort) $($Context.Destination) tail -f '$($Paths.Run)/run.log'"
    Write-Output "Results: scp -P $($Context.SshPort) -r $($Context.Destination):$($Paths.Run)/results ."

    $ShouldWait = $Wait.IsPresent -or -not [string]::IsNullOrWhiteSpace($ResultsDirectory)
    if (-not $ShouldWait) { return }

    $Status = Wait-RemoteTestJob -Context $Context -RunId $RunId
    if (-not [string]::IsNullOrWhiteSpace($ResultsDirectory)) {
        Receive-RemoteDirectory -Context $Context -RemotePath (Join-RemotePath -BasePath $Paths.Run -ChildPath 'results') -LocalPath $ResultsDirectory
    }
    Write-Host "[RESULT] Remote run $RunId finished with status: $Status"
    if ($Status -ne 'passed') { throw "Remote test run failed: $RunId" }
}