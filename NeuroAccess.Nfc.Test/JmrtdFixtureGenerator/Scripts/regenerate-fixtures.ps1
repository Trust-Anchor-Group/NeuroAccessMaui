param(
	[string] $OutputDirectory
)

$ErrorActionPreference = "Stop"

$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$GeneratorDirectory = Resolve-Path (Join-Path $ScriptDirectory "..")
$RepositoryRoot = Resolve-Path (Join-Path $ScriptDirectory "..\..\..")

if ([string]::IsNullOrWhiteSpace($OutputDirectory))
{
	$OutputDirectory = Join-Path $RepositoryRoot "NeuroAccess.Nfc.Test\TestData\JMRTD"
}

$Maven = Get-Command mvn -ErrorAction SilentlyContinue
if ($null -eq $Maven)
{
	throw "Maven is required to regenerate JMRTD fixtures. Install Maven, then run this script again. Normal dotnet test runs do not require Maven."
}

$Java = Get-Command java -ErrorAction SilentlyContinue
if ($null -eq $Java)
{
	throw "Java 17 or later is required to regenerate JMRTD fixtures. Normal dotnet test runs do not require Java."
}

$Pom = Join-Path $GeneratorDirectory "pom.xml"
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

& $Maven.Source -f $Pom -DskipTests package exec:java "-Dexec.args=$OutputDirectory"
if ($LASTEXITCODE -ne 0)
{
	throw "JMRTD fixture generation failed."
}
