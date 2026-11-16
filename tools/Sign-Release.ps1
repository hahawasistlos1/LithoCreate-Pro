param([string[]]$Files,[string]$Thumbprint,[string]$SignToolPath,[string]$TimestampUrl,[switch]$PreflightOnly)
$ErrorActionPreference='Stop'
$certificate=$null
if($Thumbprint -match '^[a-fA-F0-9]{40}$'){$certificate=Get-Item -LiteralPath ('Cert:\CurrentUser\My\'+$Thumbprint) -ErrorAction SilentlyContinue}
if(-not $SignToolPath){$command=Get-Command signtool.exe -ErrorAction SilentlyContinue;if($command){$SignToolPath=$command.Source}}
$ready=$certificate -and $certificate.HasPrivateKey -and $certificate.NotAfter -gt (Get-Date) -and $certificate.EnhancedKeyUsageList.ObjectId -contains '1.3.6.1.5.5.7.3.3' -and $SignToolPath -and (Test-Path -LiteralPath $SignToolPath -PathType Leaf)
if($PreflightOnly){[PSCustomObject]@{Ready=[bool]$ready;CertificateAvailable=[bool]$certificate;SignToolAvailable=[bool]($SignToolPath -and (Test-Path -LiteralPath $SignToolPath));SignedFiles=0};return}
if(-not $ready){throw 'Vertrauenswürdiges Codesignaturzertifikat mit privatem Schlüssel im aktuellen Benutzerzertifikatsspeicher und signtool.exe erforderlich. Es wird kein Ersatzzertifikat erstellt.'}
$timestamp=[Uri]$TimestampUrl
if(-not $timestamp.IsAbsoluteUri -or $timestamp.Scheme -ne 'https'){throw 'Für den ausdrücklich gewählten Zeitstempeldienst eine HTTPS-URL angeben.'}
if(-not $Files -or $Files.Count -eq 0){throw 'Keine Signierdateien angegeben.'}
foreach($file in $Files){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Signierdatei fehlt: $file"}}
foreach($file in $Files){
    & $SignToolPath sign /sha1 $Thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $file
    if($LASTEXITCODE -ne 0){throw "Signieren fehlgeschlagen: $file"}
    & $SignToolPath verify /pa /tw $file
    if($LASTEXITCODE -ne 0){throw "Signaturprüfung fehlgeschlagen: $file"}
    $signature=Get-AuthenticodeSignature -LiteralPath $file
    if($signature.Status -ne 'Valid' -or -not $signature.TimeStamperCertificate){throw "Keine gültige zeitgestempelte Signatur: $file"}
}
