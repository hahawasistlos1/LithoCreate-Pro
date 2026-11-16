param([Parameter(Mandatory)][string]$MsiPath,[Parameter(Mandatory)][string]$PreviousMsiPath,[Parameter(Mandatory)][string]$ReferenceMsiPath,[Parameter(Mandatory)][string]$PreviousReferenceMsiPath,[Parameter(Mandatory)][string]$TestRoot,[ValidateRange(1,20)][int]$Cycles=3)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$target=[IO.Path]::GetFullPath($TestRoot)
if(-not $target.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Das Testziel muss innerhalb des Arbeitsordners liegen.'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
$results=@()
for($cycle=1;$cycle -le $Cycles;$cycle++){
    & (Join-Path $PSScriptRoot 'Test-Installer.ps1') -MsiPath $MsiPath -PreviousMsiPath $PreviousMsiPath -TestRoot (Join-Path $target "Cycle-$cycle") -Desktop -IsolatedIdentity -ReferenceMsiPath $ReferenceMsiPath -PreviousReferenceMsiPath $PreviousReferenceMsiPath | Tee-Object -FilePath (Join-Path $target "Cycle-$cycle.txt")
    $results+=[PSCustomObject]@{Cycle=$cycle;Result='PASS'}
    $results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $target 'results.json')
}
Write-Output "PASS: $Cycles isolierte Upgrade-/Rückstufungs-/Deinstallationszyklen."
