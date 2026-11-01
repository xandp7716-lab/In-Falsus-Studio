param([string]$Target='E:\RhythmGame\Infstudio\InFalsusStudio_1.0.10',[switch]$Preflight,[switch]$InjectFailure)
$ErrorActionPreference='Stop'
$taskTarget=(Resolve-Path -LiteralPath $Target).Path.TrimEnd('\')
$taskFixture=Join-Path $PSScriptRoot 'InstallFixture'
if($InjectFailure -and $taskTarget -ne $taskFixture){throw 'Failure injection is restricted to the installer fixture'}
function SafePath([string]$relative){$taskPath=[IO.Path]::GetFullPath((Join-Path $taskTarget $relative));if([IO.Path]::IsPathRooted($relative) -or !$taskPath.StartsWith($taskTarget+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Payload path leaves selected editor copy'};return $taskPath}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
$taskJournal=SafePath 'VisualUIData\installed.json'
$taskRecord=Get-Content -LiteralPath $taskJournal -Raw | ConvertFrom-Json
if($taskRecord.version -ne '1.0.69' -or $taskRecord.target -ne $taskTarget){throw 'Expected installed 1.0.69 and matching target'}
$taskDllRelative='BepInEx\plugins\InfStudioVisualUI\InfStudioVisualUI.dll'
$taskOldEntry=@($taskRecord.files | Where-Object {$_.path -eq $taskDllRelative})
if($taskOldEntry.Count -ne 1){throw 'Expected one installed plugin record'}
$taskPreviousRecordedHash=$taskOldEntry[0].sha256;$taskPreviousPayloadHash=Hash (SafePath $taskDllRelative);$taskLocalAdjustments=$false
if($taskPreviousPayloadHash -ne $taskPreviousRecordedHash){throw 'Installed 1.0.69 plugin changed since review'}
if([Reflection.AssemblyName]::GetAssemblyName((Join-Path $PSScriptRoot 'Source\InfStudioVisualUI.dll')).Version.ToString() -ne '1.0.69.0'){throw 'Unexpected new plugin version'}
if((Hash (Join-Path $PSScriptRoot 'Source\InfStudioVisualUI.dll')) -ne '8659B5F941015650664CF3A23DACD7A78F0C57CB57F9DD1F40DEF2B7D4497354'){throw 'DLL differs from tested key panel ordering restoring floor marks and judgement lines'}
$taskPayload=@([pscustomobject]@{source=(Join-Path $PSScriptRoot 'Source\InfStudioVisualUI.dll');relative=$taskDllRelative})
$taskAllowedPayload=@($taskDllRelative)
foreach($taskItem in $taskPayload){
 if($taskItem.relative -notin $taskAllowedPayload){throw 'Unexpected payload path'}
 $taskExisting=@($taskRecord.files | Where-Object {$_.path -eq $taskItem.relative});$taskExistingPath=SafePath $taskItem.relative
 if(Test-Path -LiteralPath $taskExistingPath){if($taskExisting.Count -ne 1 -or (Hash $taskExistingPath) -ne $taskExisting[0].sha256){throw ('Payload target changed since installed record: '+$taskItem.relative)}}elseif($taskExisting.Count){throw ('Recorded payload missing: '+$taskItem.relative)}
 $taskItem | Add-Member -NotePropertyName hash -NotePropertyValue (Hash $taskItem.source)
}
$taskProtected=@('InFalsusStudio.exe','InFalsusStudio_Data\Managed\Assembly-CSharp.dll','BepInEx\config\BepInEx.cfg','BepInEx\config\local.infalsusstudio.visualui.cfg','BepInEx\plugins\InfStudioVisualUI\Assets\sidewall-gamma.bundle')
foreach($taskFolder in @('ExternalTools','BepInEx\plugins\InfStudioVisualUI\Presentation','BepInEx\plugins\InfStudioVisualUI\Assets')){foreach($taskFile in Get-ChildItem -LiteralPath (SafePath $taskFolder) -File -Recurse){$taskRelative=$taskFile.FullName.Substring($taskTarget.Length+1);if($taskRelative -notin $taskAllowedPayload){$taskProtected+=@($taskRelative)}}}
$taskProtected=@($taskProtected|Where-Object {$_ -notin $taskAllowedPayload})
$taskBefore=@{};foreach($taskPath in $taskProtected){$taskBefore[$taskPath]=Hash (SafePath $taskPath)}
$taskRunning=@(Get-Process -Name InFalsusStudio -ErrorAction SilentlyContinue | Where-Object {(!$_.Path -and $taskTarget -ne $taskFixture) -or $_.Path -eq (Join-Path $taskTarget 'InFalsusStudio.exe')})
if($Preflight){[pscustomobject]@{target=$taskTarget;version='1.0.69';editorRunning=($taskRunning.Count -gt 0);payloadFiles=$taskPayload.Count;packageCreated=$false;externalToolCapacitiesRetained=$true};return}
if($taskRunning.Count -gt 0){throw 'Save and close the selected editor copy before installing'}
$taskBackupRelative='VisualUIData\backups\'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-infstudioplus-key_layering_20261028'
$taskBackup=SafePath $taskBackupRelative
New-Item -ItemType Directory -Path $taskBackup | Out-Null
Copy-Item -LiteralPath (SafePath 'BepInEx\config\local.infalsusstudio.visualui.cfg') -Destination (Join-Path $taskBackup 'previous-visualui.cfg')
$taskOldJournal=Join-Path $taskBackup 'previous-installation.json'
if($taskLocalAdjustments){Copy-Item -LiteralPath $taskJournal -Destination (Join-Path $taskBackup 'previous-installation-original-record.json');[IO.File]::WriteAllText($taskOldJournal,($taskRecord | ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))}else{Copy-Item -LiteralPath $taskJournal -Destination $taskOldJournal}
$taskDone=[Collections.Generic.List[object]]::new();$taskTemps=[Collections.Generic.List[string]]::new();$taskJournalDone=$false
try{
 foreach($taskFile in $taskRecord.files){$taskFile.changed=$false;$taskFile.owned=$false;$taskFile.backup=$null}
 foreach($taskItem in $taskPayload){
  $taskDest=SafePath $taskItem.relative;$taskTemp=$taskDest+'.infstudioplus-key_layering_20261028.tmp';if(Test-Path -LiteralPath $taskTemp){throw 'Temporary payload exists'};$taskTemps.Add($taskTemp)
  $taskHad=Test-Path -LiteralPath $taskDest;$taskOldRelative=$taskBackupRelative+'\files\'+$taskItem.relative;$taskOld=SafePath $taskOldRelative
  New-Item -ItemType Directory -Path (Split-Path $taskDest -Parent) -Force | Out-Null
  Copy-Item -LiteralPath $taskItem.source -Destination $taskTemp
  if((Hash $taskTemp) -ne $taskItem.hash){throw 'Temporary payload checksum mismatch'}
  if($taskHad){New-Item -ItemType Directory -Path (Split-Path $taskOld -Parent) -Force | Out-Null;[IO.File]::Replace($taskTemp,$taskDest,$taskOld)}else{[IO.File]::Move($taskTemp,$taskDest)}
  $taskDone.Add([pscustomobject]@{path=$taskDest;had=$taskHad;backup=$taskOld})
  $taskEntry=@($taskRecord.files | Where-Object {$_.path -eq $taskItem.relative})
  if($taskEntry.Count -gt 1){throw 'Duplicate installed path'}
  if($taskEntry.Count -eq 1){$taskEntry=$taskEntry[0];$taskEntry.sha256=$taskItem.hash;$taskEntry.changed=$true;$taskEntry.owned=!$taskHad;$taskEntry.backup=if($taskHad){$taskOldRelative}else{$null}}
  else{$taskRecord.files=@($taskRecord.files)+@([pscustomobject]@{path=$taskItem.relative;sha256=$taskItem.hash;changed=$true;owned=!$taskHad;backup=if($taskHad){$taskOldRelative}else{$null}})}
 }
 if($InjectFailure){throw 'Intentional installer fixture failure after payload'}
 $taskRecord.version='1.0.69';$taskRecord.backup=$taskBackupRelative;$taskRecord.installedAt=[DateTime]::UtcNow.ToString('o');$taskRecord.editorLaunched=$false
 $taskRecord | Add-Member -NotePropertyName project -NotePropertyValue 'infstudioplus' -Force
 $taskRecord | Add-Member -NotePropertyName productName -NotePropertyValue 'InFalsus Studio+' -Force
 $taskRecord | Add-Member -NotePropertyName displayVersion -NotePropertyValue '1.0.69' -Force
 $taskRecord | Add-Member -NotePropertyName installationKind -NotePropertyValue 'InFalsus Studio+ 1.0.69 key panel ordering restoring floor marks and judgement lines' -Force
 $taskTempJournal=$taskJournal+'.infstudioplus-key_layering_20261028.tmp';$taskTemps.Add($taskTempJournal);[IO.File]::WriteAllText($taskTempJournal,($taskRecord | ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false));[IO.File]::Replace($taskTempJournal,$taskJournal,(Join-Path $taskBackup 'journal-replaced.json'));$taskJournalDone=$true
 foreach($taskItem in $taskPayload){if((Hash (SafePath $taskItem.relative)) -ne $taskItem.hash){throw 'Installed checksum mismatch'}}
 foreach($taskPath in $taskProtected){if((Hash (SafePath $taskPath)) -ne $taskBefore[$taskPath]){throw ('Protected file changed: '+$taskPath)}}
 $taskResult=[ordered]@{version='1.0.69';project='infstudioplus';productName='InFalsus Studio+';displayVersion='1.0.69';target=$taskTarget;backup=$taskBackup;payloadFiles=$taskPayload.Count;updatedFiles=$taskPayload.relative;protectedFiles=$taskBefore;packageCreated=$false;editorLaunched=$false;previousPayloadHash=$taskPreviousPayloadHash;previousRecordedHash=$taskPreviousRecordedHash;externalToolCapacitiesRetained=$true;timingPerformanceFixesRetained=$true}
 $taskResult | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskBackup 'installation-verification.json') -Encoding utf8
 if($taskTarget -ne $taskFixture){$taskResult | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'installation-verification.json') -Encoding utf8}
 Write-Output ('Installed InFalsus Studio+ 1.0.69 (build 20261028): '+$taskTarget);Write-Output ('Backup: '+$taskBackup)
}catch{
 for($taskIndex=$taskDone.Count-1;$taskIndex -ge 0;$taskIndex--){$taskStep=$taskDone[$taskIndex];if($taskStep.had){Copy-Item -LiteralPath $taskStep.backup -Destination $taskStep.path -Force}else{Remove-Item -LiteralPath $taskStep.path}}
 if($taskJournalDone){Copy-Item -LiteralPath $taskOldJournal -Destination $taskJournal -Force}
 throw
}finally{foreach($taskTemp in $taskTemps){if(Test-Path -LiteralPath $taskTemp){Remove-Item -LiteralPath $taskTemp}}}
