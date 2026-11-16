param(
    [Parameter(Mandatory)][string]$MsiPath,
    [Parameter(Mandatory)][string]$OutputPath,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$ReleaseVersion
)

$ErrorActionPreference='Stop'
$package=(Resolve-Path -LiteralPath $MsiPath).Path
$output=[IO.Path]::GetFullPath($OutputPath)
$workspace=Split-Path -Parent $PSScriptRoot
$icon=Join-Path $workspace 'src/LithoCreate.App/Assets/LithoCreate.ico'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(-not (Test-Path -LiteralPath $compiler)){throw 'Der Windows-.NET-Framework-Compiler für die Setup-EXE fehlt.'}

$stage=Join-Path ([IO.Path]::GetDirectoryName($output)) ('setup-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
$metadata=Join-Path $stage 'ProductInfo.cs'
$tempExe=Join-Path $stage 'Setup.exe'
try {
    @"
using System.Reflection;
[assembly: AssemblyTitle("LithoCreate Pro Setup")]
[assembly: AssemblyProduct("LithoCreate Pro")]
[assembly: AssemblyFileVersion("$ReleaseVersion.0")]
[assembly: AssemblyVersion("$ReleaseVersion.0")]
"@ | Set-Content -LiteralPath $metadata -Encoding UTF8
    & $compiler '/nologo' '/target:winexe' '/platform:x64' '/optimize+' '/r:System.Windows.Forms.dll' "/out:$tempExe" "/win32icon:$icon" "/resource:$package,LithoCreate.Setup.msi" (Join-Path $PSScriptRoot 'SetupLauncher.cs') $metadata
    if($LASTEXITCODE -ne 0){throw 'Die Setup-EXE konnte nicht gebaut werden.'}
    Move-Item -LiteralPath $tempExe -Destination $output -Force
    Write-Output "Setup-EXE erstellt: $output"
}
finally {
    if(Test-Path -LiteralPath $tempExe){Remove-Item -LiteralPath $tempExe}
    if(Test-Path -LiteralPath $metadata){Remove-Item -LiteralPath $metadata}
    [IO.Directory]::Delete($stage)
}
