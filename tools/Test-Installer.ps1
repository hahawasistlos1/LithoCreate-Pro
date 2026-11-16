param([string]$MsiPath, [string]$TestRoot, [switch]$Desktop, [string]$PreviousMsiPath, [switch]$IsolatedIdentity, [string]$ReferenceMsiPath, [string]$PreviousReferenceMsiPath)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'InstallerNative.ps1')
$package=(Resolve-Path -LiteralPath $MsiPath).Path
$product=[LithoCreateInstallerApi]::Property($package,'ProductCode')
$version=[LithoCreateInstallerApi]::Property($package,'ProductVersion')
$linkName='LithoCreate Pro'
$protectedLinks=@{}
if($IsolatedIdentity){
    $linkName='LithoCreate Pro Installertest'
    function Assert-Isolated([string]$candidate,[string]$reference){
        if(-not $reference){throw 'Ein isolierter Test benötigt das unveränderte Referenz-MSI.'}
        $original=(Resolve-Path -LiteralPath $reference).Path
        if([LithoCreateInstallerApi]::Property($candidate,'ProductName') -ne 'LithoCreate Pro Installertest' -or [LithoCreateInstallerApi]::Property($original,'ProductName') -ne 'LithoCreate Pro'){throw 'Die Produktnamen bestätigen keine getrennte Testkennung.'}
        foreach($property in @('ProductCode','UpgradeCode')){if([LithoCreateInstallerApi]::Property($candidate,$property) -eq [LithoCreateInstallerApi]::Property($original,$property)){throw 'Test- und Benutzerprodukt teilen eine Produkt-/Upgradekennung.'}}
        $productionComponents=[Collections.Generic.HashSet[string]]::new([string[]][LithoCreateInstallerApi]::Components($original))
        foreach($id in [LithoCreateInstallerApi]::Components($candidate)){if($productionComponents.Contains($id)){throw 'Test und Benutzerprodukt teilen eine Komponentenkennung.'}}
    }
    Assert-Isolated $package $ReferenceMsiPath
    foreach($protected in @((Join-Path ([Environment]::GetFolderPath('Programs')) 'LithoCreate Pro\LithoCreate Pro.lnk'),(Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'LithoCreate Pro.lnk'))){if(Test-Path -LiteralPath $protected){$protectedLinks[$protected]=(Get-FileHash -LiteralPath $protected).Hash}}
    Write-Output 'PASS: Getrennte Produkt-, Upgrade- und Komponentenkennungen für den Installertest.'
}
if([LithoCreateInstallerApi]::ProductState($product) -ne -1){throw 'Die Zielversion ist bereits installiert. Vorhandene Benutzerinstallationen werden durch den Test nicht verändert.'}
$previousPackage=$null
$previousProduct=$null
if($PreviousMsiPath){
    $previousPackage=(Resolve-Path -LiteralPath $PreviousMsiPath).Path
    if($IsolatedIdentity){Assert-Isolated $previousPackage $PreviousReferenceMsiPath}
    $previousProduct=[LithoCreateInstallerApi]::Property($previousPackage,'ProductCode')
    if($previousProduct -eq $product -or [LithoCreateInstallerApi]::ProductState($previousProduct) -ne -1){throw 'Die vorherige Version ist bereits installiert oder hat denselben Produktcode. Benutzerinstallationen bleiben unangetastet.'}
}
$testTarget=[IO.Path]::GetFullPath($TestRoot)
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(-not $testTarget.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Das Testziel muss innerhalb des Projektordners liegen.'}
New-Item -ItemType Directory -Path $testTarget -Force | Out-Null
$installTarget=Join-Path $testTarget 'App'
$installLog=Join-Path $testTarget 'install.log'
$uninstallLog=Join-Path $testTarget 'uninstall.log'
$desktopLink=Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) ($linkName+'.lnk')
if(Test-Path -LiteralPath $desktopLink){throw 'Eine vorhandene Desktopverknüpfung wird durch den Test nicht verändert.'}
$link=Join-Path ([Environment]::GetFolderPath('Programs')) ($linkName+'\'+$linkName+'.lnk')
if(Test-Path -LiteralPath $link){throw 'Eine vorhandene Startmenüverknüpfung wird durch den Test nicht verändert.'}
$preservedProject=Join-Path $testTarget 'Benutzerprojekt.lithopro'
$projectExample=Join-Path $workspace "artifacts\tests-$version\example.lithopro"
if($previousPackage -or -not(Test-Path -LiteralPath $projectExample)){$projectExample=Join-Path $workspace 'artifacts\tests\example.lithopro'}
Copy-Item -LiteralPath $projectExample -Destination $preservedProject -Force
$projectHash=(Get-FileHash -LiteralPath $preservedProject).Hash
function Run-Msi([string[]]$Arguments) {
    $process=Start-Process -FilePath msiexec.exe -ArgumentList $Arguments -WindowStyle Hidden -PassThru -Wait
    if($process.ExitCode -notin @(0,3010)){throw "Windows Installer: $($process.ExitCode). Log in $testTarget"}
}
try {
    if($previousPackage){
        Run-Msi -Arguments @('/i',('"'+$previousPackage+'"'),'/qn',('INSTALLDIR="'+$installTarget+'"'),'DESKTOP=0','/L*v',('"'+(Join-Path $testTarget 'previous-install.log')+'"'))
        Write-Output 'PASS: Vorherige Version im Testordner installiert.'
    }
    Run-Msi -Arguments @('/i',('"'+$package+'"'),'/qn',('INSTALLDIR="'+$installTarget+'"'),('DESKTOP='+[int]$Desktop.IsPresent),'/L*v',('"'+$installLog+'"'))
    if(-not (Test-Path -LiteralPath (Join-Path $installTarget 'LithoCreate Pro.exe'))){throw 'Die installierte EXE fehlt.'}
    if(-not (Test-Path -LiteralPath (Join-Path $installTarget 'coreclr.dll'))){throw 'Die integrierte Laufzeit fehlt.'}
    if(-not (Test-Path -LiteralPath $link)){throw 'Der Startmenüeintrag fehlt.'}
    if($Desktop -and -not (Test-Path -LiteralPath $desktopLink)){throw 'Die gewählte Desktopverknüpfung fehlt.'}
    $publishedApp=Join-Path $workspace "artifacts\portable-$version"
    $releaseFiles=@('LithoCreate Pro.dll','LithoCreate.Core.dll')
    if([version]$version -ge [version]'1.20.0'){$releaseFiles+=@('LithoCreate.PluginHost.exe','LithoCreate.PluginHost.dll','Extensions\LithoCreate.WaveForm.dll','Extensions\LithoCreate.DrilledPhoto.dll','Probedrucke\P1S-PETG-Probedrucke.zip')}
    foreach($binaryName in $releaseFiles){
        $publishedBinary=Join-Path $publishedApp $binaryName
        $installedBinary=Join-Path $installTarget $binaryName
        if((Get-FileHash -LiteralPath $publishedBinary).Hash -ne (Get-FileHash -LiteralPath $installedBinary).Hash){throw "Die installierte Bibliothek ist nicht bytegleich mit dem Release: $binaryName"}
    }
    Write-Output 'PASS: Anwendung, Core und vorhandene Modul-/Probedruckdateien nach Upgrade bytegenau installiert.'
    $sourceExamples=Join-Path $workspace "artifacts\portable-$version\Beispiele"
    if(Test-Path -LiteralPath $sourceExamples){
        foreach($example in Get-ChildItem -LiteralPath $sourceExamples -Filter '*.lithopro' -File){
            $installedExample=Join-Path (Join-Path $installTarget 'Beispiele') $example.Name
            if(-not(Test-Path -LiteralPath $installedExample) -or (Get-FileHash -LiteralPath $installedExample).Hash -ne (Get-FileHash -LiteralPath $example.FullName).Hash){throw 'Ein Beispielprojekt mit Sonderzeichen im Dateinamen wurde nicht korrekt installiert.'}
        }
        Write-Output 'PASS: Beispielprojekte mit Umlauten und Sonderzeichen bytegenau installiert.'
    }
    Get-ChildItem -LiteralPath $installTarget -File -Recurse | Measure-Object | Format-List
    Write-Output 'PASS: Installation, Laufzeit und Startmenüeintrag.'
    $installedInfo=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $installTarget 'LithoCreate Pro.dll'))
    if("$($installedInfo.FileMajorPart).$($installedInfo.FileMinorPart).$($installedInfo.FileBuildPart)" -ne $version){throw 'Die installierte DLL hat eine falsche Version.'}
    if($previousPackage){
        if([LithoCreateInstallerApi]::ProductState($previousProduct) -ne -1){throw 'Die alte Version wurde beim Upgrade nicht entfernt.'}
        Write-Output 'PASS: Upgrade installiert neue Dateien und entfernt die alte Produktregistrierung.'
        $downgradeArgs=@('/i',('"'+$previousPackage+'"'),'/qn','/L*v',('"'+(Join-Path $testTarget 'downgrade.log')+'"'))
        $downgrade=Start-Process -FilePath msiexec.exe -ArgumentList $downgradeArgs -WindowStyle Hidden -PassThru -Wait
        if($downgrade.ExitCode -ne 1603){throw "Rückstufung wurde nicht wie erwartet abgewiesen: $($downgrade.ExitCode)"}
        if([LithoCreateInstallerApi]::ProductState($product) -ne 5){throw 'Die abgewiesene Rückstufung hat die neue Installation verändert.'}
        Write-Output 'PASS: Rückstufung auf die alte Version wird ohne Veränderung der neuen Installation abgewiesen.'
    }
    $projectInApp=Join-Path $installTarget 'Persoenliches-Projekt.lithopro'
    Copy-Item -LiteralPath $preservedProject -Destination $projectInApp -Force
}
finally {
    if([LithoCreateInstallerApi]::ProductState($product) -ne -1){Run-Msi -Arguments @('/x',('"'+$package+'"'),'/qn','/L*v',('"'+$uninstallLog+'"'))}
    if($previousPackage -and [LithoCreateInstallerApi]::ProductState($previousProduct) -ne -1){Run-Msi -Arguments @('/x',('"'+$previousPackage+'"'),'/qn','/L*v',('"'+(Join-Path $testTarget 'previous-uninstall.log')+'"'))}
}
if(Test-Path -LiteralPath (Join-Path $installTarget 'LithoCreate Pro.exe')){throw 'Die EXE wurde bei der Deinstallation nicht entfernt.'}
if(Test-Path -LiteralPath $link){throw 'Der Startmenüeintrag wurde bei der Deinstallation nicht entfernt.'}
if(Test-Path -LiteralPath $desktopLink){throw 'Die Desktopverknüpfung wurde bei der Deinstallation nicht entfernt.'}
if((Get-FileHash -LiteralPath $preservedProject).Hash -ne $projectHash){throw 'Das Benutzerprojekt wurde verändert.'}
if((Get-FileHash -LiteralPath $projectInApp).Hash -ne $projectHash){throw 'Ein persönlich im Programmordner gespeichertes Projekt wurde gelöscht oder verändert.'}
Write-Output 'PASS: Saubere Deinstallation der Programmdateien und Verknüpfung.'
Write-Output 'PASS: Persönliche Projektdateien außerhalb und innerhalb des Programmordners unverändert erhalten.'
foreach($protected in $protectedLinks.Keys){if(-not(Test-Path -LiteralPath $protected) -or (Get-FileHash -LiteralPath $protected).Hash -ne $protectedLinks[$protected]){throw 'Eine vorhandene Benutzerverknüpfung wurde verändert.'}}
if($IsolatedIdentity){Write-Output 'PASS: Vorhandene Benutzerverknüpfungen bytegenau erhalten.'}
