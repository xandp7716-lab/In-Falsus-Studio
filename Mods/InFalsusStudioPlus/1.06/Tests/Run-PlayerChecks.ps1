param([Parameter(Mandatory=$true)][string]$Editor,[Parameter(Mandatory=$true)][string]$TestHost,[ValidateSet('focus','brand')][string]$Suite='focus')
$ErrorActionPreference='Stop'
$editorPath=(Resolve-Path -LiteralPath $Editor).Path.TrimEnd('\');$hostPath=(Resolve-Path -LiteralPath $TestHost).Path.TrimEnd('\')
if($editorPath -eq $hostPath -or $hostPath.StartsWith($editorPath+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Use a separate, isolated editor copy as TestHost'}
if(@(Get-Process -Name InFalsusStudio -ErrorAction SilentlyContinue | Where-Object {!$_.Path -or $_.Path -eq (Join-Path $hostPath 'InFalsusStudio.exe')}).Count){throw 'Close the test host before running checks'}
$name=if($Suite -eq 'focus'){'FocusChecks'}else{'BrandChecks'};$flag=if($Suite -eq 'focus'){'-focus-checks'}else{'-brand-checks'}
$release=Split-Path $PSScriptRoot -Parent;$plugin=Join-Path $release 'Source\InfStudioVisualUI.dll'
if(!(Test-Path -LiteralPath $plugin)){throw 'Build the 1.06 integrated plugin first'}
$references=@('Assembly-CSharp.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.AudioModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.ScreenCaptureModule.dll','UnityEngine.ImageConversionModule.dll','netstandard.dll') | ForEach-Object {'/reference:'+(Join-Path $editorPath ('InFalsusStudio_Data\Managed\'+$_))}
$references+=@('0Harmony.dll','BepInEx.Core.dll','BepInEx.Unity.Mono.dll') | ForEach-Object {'/reference:'+(Join-Path $editorPath ('BepInEx\core\'+$_))}
$output=Join-Path $PSScriptRoot ('Results\'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$Suite);New-Item -ItemType Directory -Path $output -Force | Out-Null
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:library ('/out:'+(Join-Path $output ($name+'.dll'))) $references (Join-Path $PSScriptRoot ($name+'.cs'))
if($LASTEXITCODE -ne 0){throw 'Player check plugin compilation failed'}
$pluginDir=Join-Path $hostPath 'BepInEx\plugins\InfStudioVisualUI';New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
Copy-Item -LiteralPath $plugin -Destination (Join-Path $pluginDir 'InfStudioVisualUI.dll') -Force
Copy-Item -LiteralPath (Join-Path $output ($name+'.dll')) -Destination (Join-Path $hostPath ('BepInEx\plugins\'+$name+'.dll')) -Force
$process=Start-Process -FilePath (Join-Path $hostPath 'InFalsusStudio.exe') -WorkingDirectory $hostPath -WindowStyle Hidden -ArgumentList @($flag,('"'+$output+'"'),'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',('"'+$output+'\Player.log"')) -PassThru
[pscustomobject]@{processId=$process.Id;results=$output;suite=$Suite}
