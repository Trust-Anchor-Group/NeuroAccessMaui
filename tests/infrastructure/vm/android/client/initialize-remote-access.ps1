[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $HostName,
    [string] $UserName = "neuro-test",
    [string] $KeyPath = (Join-Path $HOME ".ssh\id_ed25519")
)

$ErrorActionPreference = "Stop"
if ($HostName -notmatch '^[A-Za-z0-9.-]+$') { throw "HostName contains unsupported characters." }
if ($UserName -notmatch '^[A-Za-z0-9._-]+$') { throw "UserName contains unsupported characters." }
foreach ($CommandName in @("ssh", "ssh-keygen")) {
    if ($null -eq (Get-Command $CommandName -ErrorAction SilentlyContinue)) { throw "Required command is missing: $CommandName" }
}

$KeyDirectory = Split-Path -Parent $KeyPath
[void](New-Item -ItemType Directory -Path $KeyDirectory -Force)
if (-not (Test-Path -LiteralPath $KeyPath -PathType Leaf)) {
    $EmptyPassphraseArgument = '""'
    & ssh-keygen -t ed25519 -f $KeyPath -N $EmptyPassphraseArgument -C "neuro-test-$env:COMPUTERNAME"
    if ($LASTEXITCODE -ne 0) { throw "Could not create the SSH key." }
}
$PublicKeyPath = "$KeyPath.pub"
if (-not (Test-Path -LiteralPath $PublicKeyPath -PathType Leaf)) {
    $PublicKey = (& ssh-keygen -y -f $KeyPath) -join ""
    [System.IO.File]::WriteAllText($PublicKeyPath, $PublicKey + "`n", [System.Text.UTF8Encoding]::new($false))
}
$PublicKey = (Get-Content -LiteralPath $PublicKeyPath -Raw).Trim()
if ($PublicKey -notmatch '^ssh-ed25519\s+[A-Za-z0-9+/=]+(?:\s+.*)?$') { throw "The public SSH key has an unexpected format." }

$Remote = "$UserName@$HostName"
Write-Host "Enter the remote password once to authorize this computer."
$InstallCommand = 'umask 077; mkdir -p .ssh; cat >> .ssh/authorized_keys; sed -i "s/\r$//" .ssh/authorized_keys; sort -u .ssh/authorized_keys -o .ssh/authorized_keys; chmod 700 .ssh; chmod 600 .ssh/authorized_keys'
($PublicKey + "`n") | & ssh $Remote $InstallCommand
if ($LASTEXITCODE -ne 0) { throw "Could not install the SSH public key." }

& ssh -o BatchMode=yes -i $KeyPath $Remote "true"
if ($LASTEXITCODE -ne 0) { throw "Key authentication verification failed." }
Write-Output "SSH key authentication is ready for $Remote."
Write-Output "Private key: $KeyPath"