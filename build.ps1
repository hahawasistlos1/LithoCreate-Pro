param([switch]$SkipTests, [switch]$SkipInstaller, [switch]$SkipZip, [string]$SigningThumbprint, [string]$SignToolPath, [string]$TimestampUrl)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='true'
$env:MSBuildEnableWorkloadResolver='false'
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '.build-home'
[xml]$appProject=Get-Content -LiteralPath 'src/LithoCreate.App/LithoCreate.App.csproj' -Raw
$releaseVersion=[string]($appProject.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)
if($releaseVersion -notmatch '^\d+\.\d+\.\d+$'){throw 'Die App-Projektdatei muss eine dreiteilige Versionsnummer enthalten.'}
$portable=Join-Path 'artifacts' "portable-$releaseVersion"
$installer=Join-Path 'artifacts' "LithoCreate-Pro-$releaseVersion-win-x64.msi"
$setup=Join-Path 'artifacts' "LithoCreate-Pro-$releaseVersion-Setup.exe"
$archive=Join-Path 'artifacts' "LithoCreate-Pro-$releaseVersion-portabel.zip"
New-Item -ItemType Directory -Path 'artifacts' -Force | Out-Null
function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if($LASTEXITCODE -ne 0){throw "dotnet ist mit Code $LASTEXITCODE fehlgeschlagen."}
}
Invoke-Dotnet -Arguments @('build','LithoCreatePro.sln','-c','Release','-m:1','-p:UseSharedCompilation=false','-p:NuGetAudit=false','-v','minimal')
if(-not $SkipTests){
    & dotnet run --project tests/LithoCreate.Tests -c Release --no-build -- "artifacts/tests-$releaseVersion" | Tee-Object -FilePath "artifacts/test-results-$releaseVersion.txt"
    if($LASTEXITCODE -ne 0){throw 'Integrationstests fehlgeschlagen.'}
}
Invoke-Dotnet -Arguments @('publish','src/LithoCreate.App','-c','Release','-r','win-x64','--self-contained','true','-p:UseSharedCompilation=false','-p:BuildInParallel=false','-p:NuGetAudit=false','-o',$portable,'-v','minimal')
Invoke-Dotnet -Arguments @('publish','tools/LithoCreate.PluginHost','-c','Release','-r','win-x64','--self-contained','true','-p:UseSharedCompilation=false','-p:BuildInParallel=false','-p:NuGetAudit=false','-o',$portable,'-v','minimal')
$appMetadata=Get-Content -LiteralPath (Join-Path $portable 'LithoCreate Pro.deps.json') -Raw | ConvertFrom-Json -AsHashtable
$packageRoot=$env:NUGET_PACKAGES
if(-not $packageRoot){$packageRoot=Join-Path $env:DOTNET_CLI_HOME '.nuget/packages'}
foreach($name in @('Microsoft.NETCore.App.Runtime.win-x64','Microsoft.WindowsDesktop.App.Runtime.win-x64')){
    $library=$appMetadata.libraries.Keys | Where-Object { $_ -like "runtimepack.$name/*" } | Select-Object -First 1
    if(-not $library){throw "Laufzeitpaket fehlt in den Veröffentlichungsdaten: $name"}
    $packVersion=($library -split '/')[1]
    $pack=Join-Path (Join-Path $packageRoot $name.ToLowerInvariant()) $packVersion
    $notices=Join-Path $portable ('Lizenzen/'+$name)
    New-Item -ItemType Directory -Path $notices -Force | Out-Null
    $licenseFiles=Get-ChildItem -LiteralPath $pack -File | Where-Object Name -Match '^(LICENSE|THIRD-PARTY-NOTICES)(\.TXT)?$'
    if(-not $licenseFiles){throw "Lizenzdateien fehlen im Laufzeitpaket: $name"}
    foreach($file in $licenseFiles){Copy-Item -LiteralPath $file.FullName -Destination $notices -Force}
}
New-Item -ItemType Directory -Path (Join-Path $portable 'Extensions') -Force | Out-Null
Copy-Item -LiteralPath 'extensions/WaveForm/bin/Release/net10.0/LithoCreate.WaveForm.dll' -Destination (Join-Path $portable 'Extensions/LithoCreate.WaveForm.dll') -Force
Copy-Item -LiteralPath 'extensions/DrilledPhoto/bin/Release/net10.0/LithoCreate.DrilledPhoto.dll' -Destination (Join-Path $portable 'Extensions/LithoCreate.DrilledPhoto.dll') -Force
if($SigningThumbprint){
    $owned=Get-ChildItem -LiteralPath $portable -File -Recurse | Where-Object {$_.Name -like 'LithoCreate*' -and $_.Extension -in @('.exe','.dll')}
    & ./tools/Sign-Release.ps1 -Files $owned.FullName -Thumbprint $SigningThumbprint -SignToolPath $SignToolPath -TimestampUrl $TimestampUrl
}
Copy-Item -LiteralPath 'docs' -Destination $portable -Recurse -Force
Copy-Item -LiteralPath 'README.md' -Destination (Join-Path $portable 'README.md') -Force
Invoke-Dotnet -Arguments @('run','--project','tests/LithoCreate.Tests','-c','Release','--no-build','--','--examples',(Join-Path $portable 'Beispiele'))
New-Item -ItemType Directory -Path (Join-Path $portable 'Probedrucke') -Force | Out-Null
Invoke-Dotnet -Arguments @('run','--project','tests/LithoCreate.Tests','-c','Release','--no-build','--','--kit',(Join-Path $portable 'Probedrucke/P1S-PETG-Probedrucke.zip'))
if(-not $SkipInstaller){Invoke-Dotnet -Arguments @('run','--project','tools/LithoCreate.Packager','-c','Release','-p:UseSharedCompilation=false','-p:NuGetAudit=false','--',$portable,$installer)}
if($SigningThumbprint -and -not $SkipInstaller){& ./tools/Sign-Release.ps1 -Files @($installer) -Thumbprint $SigningThumbprint -SignToolPath $SignToolPath -TimestampUrl $TimestampUrl}
if(-not $SkipInstaller){
    & ./tools/Build-SetupExe.ps1 -MsiPath $installer -OutputPath $setup -ReleaseVersion $releaseVersion
    if($SigningThumbprint){& ./tools/Sign-Release.ps1 -Files @($setup) -Thumbprint $SigningThumbprint -SignToolPath $SignToolPath -TimestampUrl $TimestampUrl}
}
if(-not $SkipZip){
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if(Test-Path -LiteralPath $archive){Remove-Item -LiteralPath $archive}
    [IO.Compression.ZipFile]::CreateFromDirectory((Join-Path $PSScriptRoot $portable),(Join-Path $PSScriptRoot $archive),[IO.Compression.CompressionLevel]::Optimal,$false)
}
$outputs=@($setup,$installer,$archive) | Where-Object { Test-Path -LiteralPath $_ }
if($outputs){Get-FileHash -LiteralPath $outputs -Algorithm SHA256 | Format-List Algorithm,Hash,Path | Out-String | Set-Content -LiteralPath "artifacts/SHA256-$releaseVersion.txt"}
Write-Host "Fertig: $portable, $installer und $setup"
