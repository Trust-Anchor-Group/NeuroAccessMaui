# JMRTD Fixture Generator

This opt-in Java helper generates synthetic LDS files for the .NET test suite
using JMRTD. Normal `dotnet test` runs do not invoke Java, Maven, or this
project.

Run from the repository root:

```powershell
.\NeuroAccess.Nfc.Test\JmrtdFixtureGenerator\Scripts\regenerate-fixtures.ps1
```

The generated files are written to `NeuroAccess.Nfc.Test/TestData/JMRTD`.
They contain fake data only and are used as parser and ISO-DEP download
fixtures. JMRTD jars are restored by Maven and are not committed.
