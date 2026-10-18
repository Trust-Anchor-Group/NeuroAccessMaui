[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '../..')
)

Set-StrictMode -Version Latest
$ContractRoot = (Resolve-Path -LiteralPath $RepositoryRoot -ErrorAction Stop).Path
$CatalogPath = Join-Path $ContractRoot 'NeuroAccessMaui/UI/Automation/AutomationIds.cs'
$CatalogSource = Get-Content -LiteralPath $CatalogPath -Raw -ErrorAction Stop
$Problems = [System.Collections.Generic.List[string]]::new()
$Definitions = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
$Values = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$DefinitionPattern = '(?s)/// <remarks>Owner: (?<owner>.*?)\.</remarks>\s*public const string (?<symbol>\w+) = "(?<id>[^"]+)";'
foreach ($DefinitionMatch in [regex]::Matches($CatalogSource, $DefinitionPattern)) {
    $Symbol = $DefinitionMatch.Groups['symbol'].Value
    $Identifier = $DefinitionMatch.Groups['id'].Value
    $Owner = $DefinitionMatch.Groups['owner'].Value
    if ($Definitions.ContainsKey($Symbol) -or -not $Values.Add($Identifier)) {
        $Problems.Add("Duplicate catalog definition: $Symbol")
        continue
    }
    if ($Identifier -notmatch '^(dev_)?(screen|popup|button|input|label|image|control|item)_[a-z][a-z0-9]*_[a-z][a-z0-9]*(_[a-z0-9]+)*$') {
        $Problems.Add("Invalid type/area/view namespace: $Symbol")
    }
    if (-not (Test-Path -LiteralPath (Join-Path $ContractRoot $Owner) -PathType Leaf)) {
        $Problems.Add("Missing catalog owner: $Symbol")
    }
    if ($Identifier.StartsWith('dev_', [System.StringComparison]::Ordinal) -ne
        $Owner.StartsWith('DevTestPlatform/', [System.StringComparison]::Ordinal)) {
        $Problems.Add("Production/developer namespace ownership mismatch: $Symbol")
    }
    $Definitions.Add($Symbol, [pscustomobject]@{ Id = $Identifier; Owner = $Owner; Uses = 0 })
}
$RawDefinitions = [regex]::Matches($CatalogSource, 'public const string \w+\s*=')
if ($Definitions.Count -ne $RawDefinitions.Count) { $Problems.Add('Every catalog constant must declare its owner.') }

$DynamicBlock = [regex]::Match($CatalogSource, '(?s)dynamicPrefixes\s*=.*?\{(?<body>.*?)\};').Groups['body'].Value
$DynamicSymbols = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($DynamicMatch in [regex]::Matches($DynamicBlock, '\b\w+\b')) {
    if (-not $Definitions.ContainsKey($DynamicMatch.Value)) {
        $Problems.Add("Unknown dynamic namespace: $($DynamicMatch.Value)")
    }
    [void]$DynamicSymbols.Add($DynamicMatch.Value)
}
$RetiredBlock = [regex]::Match($CatalogSource, '(?s)Retired.*?new string\[\].*?\{(?<body>.*?)\}').Groups['body'].Value
$Retired = @([regex]::Matches($RetiredBlock, '"([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
if (@($Retired | Sort-Object -Unique).Count -ne $Retired.Count) {
    $Problems.Add('Retired identifiers must be reserved exactly once.')
}
foreach ($Definition in $Definitions.GetEnumerator()) {
    foreach ($RetiredName in $Retired) {
        if ($Definition.Value.Id -ceq $RetiredName -or
            ($RetiredName.EndsWith('_') -and $Definition.Value.Id.StartsWith($RetiredName, [System.StringComparison]::Ordinal))) {
            $Problems.Add("Retired identifier reused: $($Definition.Key)")
        }
    }
}

$SourceFiles = @(foreach ($SourceRoot in @('NeuroAccessMaui', 'DevTestPlatform')) {
    Get-ChildItem -LiteralPath (Join-Path $ContractRoot $SourceRoot) -Recurse -File -ErrorAction Stop |
        Where-Object { $_.Extension -in @('.xaml', '.cs') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
})
foreach ($SourceFile in $SourceFiles) {
    $RelativePath = [System.IO.Path]::GetRelativePath($ContractRoot, $SourceFile.FullName).Replace('\', '/')
    $Source = Get-Content -LiteralPath $SourceFile.FullName -Raw -ErrorAction Stop
    if ([string]::IsNullOrEmpty($Source) -or $SourceFile.FullName -eq $CatalogPath) { continue }
    foreach ($Reference in [regex]::Matches($Source, '\bAutomationIds\.(?<symbol>\w+)')) {
        $Symbol = $Reference.Groups['symbol'].Value
        if ($Symbol -in @('ForKey', 'Retired')) { continue }
        if (-not $Definitions.ContainsKey($Symbol)) { $Problems.Add("Unknown catalog reference: $RelativePath : $Symbol") }
    }
    if ($SourceFile.Extension -eq '.xaml') {
        try { $Document = [System.Xml.Linq.XDocument]::Parse($Source) }
        catch { $Problems.Add("Invalid XAML: $RelativePath"); continue }
        $Metadata = @($Document.Descendants() | ForEach-Object {
            $_.Attributes() | Where-Object { $_.Name.LocalName -eq 'AutomationId' }
        })
        foreach ($Attribute in $Metadata) {
            $Value = $Attribute.Value
            $Static = [regex]::Match($Value, '^\{x:Static \w+:AutomationIds\.(?<symbol>\w+)\}$')
            $Dynamic = [regex]::Match($Value, 'Converter=\{\w+:(?:AutomationKey|KycFieldKey) Prefix=\{x:Static \w+:AutomationIds\.(?<symbol>\w+)\}\}')
            if ($Static.Success -or $Dynamic.Success) {
                $Symbol = if ($Static.Success) { $Static.Groups['symbol'].Value } else { $Dynamic.Groups['symbol'].Value }
                if (-not $Definitions.ContainsKey($Symbol)) { continue }
                $Definition = $Definitions[$Symbol]
                if ($Definition.Owner -cne $RelativePath) { $Problems.Add("Wrong metadata owner: $RelativePath : $Symbol") }
                if ($Dynamic.Success -and -not $DynamicSymbols.Contains($Symbol)) { $Problems.Add("Unregistered dynamic namespace: $Symbol") }
                if ($Static.Success -and $DynamicSymbols.Contains($Symbol)) { $Problems.Add("Dynamic namespace used without an instance key: $Symbol") }
                $Definition.Uses++
            } elseif ($Value -eq '{Binding AutomationId}' -and $RelativePath -eq 'DevTestPlatform/UI/Pages/CatalogPage.xaml') {
                # The catalog item model owns and encodes screen/scenario keys.
            } else { $Problems.Add("Uncatalogued automation metadata: $RelativePath") }
        }
        foreach ($Namespace in $Document.Root.Attributes() | Where-Object {
            $_.IsNamespaceDeclaration -and $_.Value.Contains('UI.Automation')
        }) {
            $ExpectedNamespace = if ($RelativePath.StartsWith('DevTestPlatform/')) {
                'clr-namespace:NeuroAccessMaui.UI.Automation;assembly=NeuroAccessMaui'
            } else { 'clr-namespace:NeuroAccessMaui.UI.Automation' }
            if ($Namespace.Value -cne $ExpectedNamespace) {
                $Problems.Add("Wrong automation namespace binding: $RelativePath")
            }
        }
        foreach ($Setter in $Document.Descendants() | Where-Object { $_.Name.LocalName -eq 'Setter' }) {
            if ($Setter.Attribute('Property') -and $Setter.Attribute('Property').Value -eq 'AutomationId') {
                $Problems.Add("AutomationId setters require explicit catalog ownership: $RelativePath")
            }
        }
    } else {
        foreach ($Assignment in [regex]::Matches($Source, '\bAutomationId\s*=\s*(?<expression>[^\r\n;]+)')) {
            $Expression = $Assignment.Groups['expression'].Value
            if ($RelativePath -eq 'NeuroAccessMaui/UI/Controls/Inputs/CompositeEntry.cs' -and $Expression.Trim() -eq 'value') {
                # Existing component forwarding is not a new logical identifier declaration.
                continue
            }
            $Reference = [regex]::Match($Expression, '\bAutomationIds\.(?<symbol>\w+)')
            if (-not $Reference.Success) { $Problems.Add("Uncatalogued C# automation assignment: $RelativePath"); continue }
            $Symbol = $Reference.Groups['symbol'].Value
            if ($Symbol -eq 'ForKey') {
                if (-not ($Definitions.Values | Where-Object { $_.Owner -eq $RelativePath -and $Source.Contains('AutomationIds.ForKey(') })) {
                    $Problems.Add("Dynamic C# assignment has no catalog owner: $RelativePath")
                }
            } elseif ($Definitions.ContainsKey($Symbol)) {
                $Definition = $Definitions[$Symbol]
                if ($Definition.Owner -cne $RelativePath) { $Problems.Add("Wrong C# metadata owner: $RelativePath : $Symbol") }
                $Definition.Uses++
            }
        }
    }
}
foreach ($Definition in $Definitions.GetEnumerator()) {
    if (-not $DynamicSymbols.Contains($Definition.Key) -and $Definition.Value.Uses -ne 1) {
        $Problems.Add("Fixed identity must have exactly one declaration: $($Definition.Key) ($($Definition.Value.Uses))")
    }
}

$TestRoot = Join-Path $ContractRoot 'tests'
$SupportRoot = Join-Path $TestRoot 'NeuroAccessMaui.Testing'
$Project = Get-Content -LiteralPath (Join-Path $SupportRoot 'NeuroAccessMaui.Testing.csproj') -Raw
if (!$Project.Contains('../NeuroAccessMaui/UI/Automation/AutomationIds.cs')) {
    $Problems.Add('The test project must link the production AutomationIds catalog.')
}
if (!$Project.Contains('../NeuroAccessMaui/Resources/Languages/AppResources*.resx')) {
    $Problems.Add('The test project must link the production localization resources.')
}
$TextKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($TestFile in Get-ChildItem -LiteralPath $TestRoot -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }) {
    $Source = Get-Content -LiteralPath $TestFile.FullName -Raw
    foreach ($Reference in [regex]::Matches($Source, '\bAutomationIds\.(?<symbol>\w+)')) {
        $Symbol = $Reference.Groups['symbol'].Value
        if ($Symbol -ne 'ForKey' -and !$Definitions.ContainsKey($Symbol)) { $Problems.Add("Unknown test catalog reference: $($TestFile.Name) : $Symbol") }
    }
    foreach ($Key in [regex]::Matches($Source, '(?:Text|Language|Translations)\["(?<key>[^"\r\n]+)"\]|MatchesNumber\("(?<key>[^"\r\n]+)"')) {
        [void]$TextKeys.Add($Key.Groups['key'].Value)
    }
}
[xml]$Resources = Get-Content -LiteralPath (Join-Path $ContractRoot 'NeuroAccessMaui/Resources/Languages/AppResources.resx') -Raw
$ResourceKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($Entry in $Resources.root.data) { [void]$ResourceKeys.Add($Entry.name) }
foreach ($Key in $TextKeys) {
    if (!$ResourceKeys.Contains($Key)) { $Problems.Add("Unknown application resource reference: $Key") }
}
if ($Problems.Count -gt 0) {
    $Problems | Sort-Object -Unique | ForEach-Object { Write-Output "CONTRACT: $_" }
    throw "Automation contract validation failed ($($Problems.Count) findings)."
}
Write-Output "Automation contract validated: $($Definitions.Count) definitions, $($DynamicSymbols.Count) dynamic namespaces. Localization resources are validated by the unit suite."
