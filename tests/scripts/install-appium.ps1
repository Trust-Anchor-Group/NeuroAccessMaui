#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('android', 'ios')][string]$Platform = 'android',
    [switch]$LegacyAndroid
)
$ErrorActionPreference = 'Stop'
$Toolchain = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../config/toolchain.json') -Raw | ConvertFrom-Json
if ($LegacyAndroid -and ($Platform -ne 'android' -or !$env:APPIUM_HOME)) { throw 'Legacy Android requires Platform android and a separate APPIUM_HOME.' }
# An explicit bootstrap command; installation is separate from test execution.
& npm install --global "appium@$($Toolchain.appium)"
if ($LASTEXITCODE -ne 0) { throw 'Appium installation failed.' }
$Driver = if ($Platform -eq 'android') { "uiautomator2@$($Toolchain.uiautomator2)" } else { "xcuitest@$($Toolchain.xcuitest)" }
if ($LegacyAndroid) { $Driver = "uiautomator2@$($Toolchain.uiautomator2Legacy)" }
& appium driver install $Driver
if ($LASTEXITCODE -ne 0) { throw 'Driver installation failed. Use a dedicated APPIUM_HOME or remove the previous driver explicitly before upgrading.' }
& appium driver doctor $(if ($Platform -eq 'android') { 'uiautomator2' } else { 'xcuitest' })
if ($LASTEXITCODE -ne 0) { throw 'Appium driver prerequisites are incomplete.' }
