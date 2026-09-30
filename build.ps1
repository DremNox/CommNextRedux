$ErrorActionPreference = 'Stop'
$project = 'C:\Users\aritz\Documents\KSP2ReduxPorts\CommNextRedux'
$game = 'C:\Users\aritz\Desktop\Kerbal.Space.Program.2.v0.2.2.0.Early.Access'
$managed = Join-Path $game 'KSP2_x64_Data\Managed'
$csc = Join-Path $project 'tools\roslyn\tasks\net472\csc.exe'
$out = Join-Path $project 'CommNextRedux.0.1.0-alpha.dll'

$refs = @(
  'Assembly-CSharp.dll',
  'ReduxLib.dll',
  'SpaceWarp2.dll',
  '0Harmony.dll',
  'UnityEngine.dll',
  'UnityEngine.CoreModule.dll',
  'UnityEngine.IMGUIModule.dll',
  'UnityEngine.InputLegacyModule.dll',
  'UnityEngine.UIElementsModule.dll',
  'UnityEngine.AssetBundleModule.dll',
  'Unity.InputSystem.dll',
  'Unity.Collections.dll',
  'Unity.Burst.dll',
  'Unity.Mathematics.dll',
  'Newtonsoft.Json.dll',
  'System.Memory.dll',
  'System.Buffers.dll',
  'System.Runtime.CompilerServices.Unsafe.dll',
  'netstandard.dll'
)

$argsList = @('/nologo','/target:library','/langversion:latest','/optimize+','/unsafe')
$argsList += '/out:' + $out
$argsList += $refs | ForEach-Object { '/reference:' + (Join-Path $managed $_) }
$argsList += Get-ChildItem (Join-Path $project 'redux-src\*.cs') | ForEach-Object { $_.FullName }
$argsList += Get-ChildItem (Join-Path $project 'full-port-src\*.cs') -Recurse | ForEach-Object { $_.FullName }

& $csc @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Output 'BUILD OK'
