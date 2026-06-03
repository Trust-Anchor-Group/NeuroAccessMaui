param()

$ErrorActionPreference = "Stop"

$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepositoryRoot = Resolve-Path (Join-Path $ScriptDirectory "..\..\..")
$CommittedDirectory = Join-Path $RepositoryRoot "NeuroAccess.Nfc.Test\TestData\JMRTD"
$TempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("neuroaccess-jmrtd-validate-" + [Guid]::NewGuid().ToString("N"))

try
{
	& (Join-Path $ScriptDirectory "regenerate-fixtures.ps1") -OutputDirectory $TempDirectory
	if ($LASTEXITCODE -ne 0)
	{
		throw "JMRTD validation generation failed."
	}

	$GeneratedFiles = Get-ChildItem -Path $TempDirectory -File -Recurse
	foreach ($GeneratedFile in $GeneratedFiles)
	{
		$RelativePath = [System.IO.Path]::GetRelativePath($TempDirectory, $GeneratedFile.FullName)
		$CommittedPath = Join-Path $CommittedDirectory $RelativePath

		if (!(Test-Path $CommittedPath))
		{
			throw "Committed fixture is missing: $RelativePath"
		}

		$GeneratedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $GeneratedFile.FullName).Hash
		$CommittedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $CommittedPath).Hash
		if ($GeneratedHash -ne $CommittedHash)
		{
			throw "Committed fixture differs from JMRTD regenerated output: $RelativePath"
		}
	}

	Write-Host "JMRTD fixture oracle validation passed."
}
finally
{
	if (Test-Path $TempDirectory)
	{
		Remove-Item -LiteralPath $TempDirectory -Recurse -Force
	}
}
