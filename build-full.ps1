$ErrorActionPreference = 'Stop'
$root = 'C:\Users\aritz\Documents\KSP2ReduxPorts\CommNextRedux'
$full = Join-Path $root 'full-port'
$game = 'C:\Users\aritz\Desktop\Kerbal.Space.Program.2.v0.2.2.0.Early.Access'
$managed = Join-Path $game 'KSP2_x64_Data\Managed'
$csc = Join-Path $root 'tools\roslyn\tasks\net472\csc.exe'
$out = Join-Path $full 'CommNextRedux.Full.0.1.0.dll'

$refs = @(
  'Assembly-CSharp.dll',
  'Assembly-CSharp-firstpass.dll',
  'ReduxLib.dll',
  'SpaceWarp2.dll',
  '0Harmony.dll',
  'UnityEngine.dll',
  'UnityEngine.CoreModule.dll',
  'UnityEngine.AssetBundleModule.dll',
  'UnityEngine.IMGUIModule.dll',
  'UnityEngine.InputLegacyModule.dll',
  'UnityEngine.InputModule.dll',
  'UnityEngine.UIElementsModule.dll',
  'Unity.InputSystem.dll',
  'Unity.Collections.dll',
  'Unity.Burst.dll',
  'Unity.Mathematics.dll',
  'System.Memory.dll',
  'UitkForKsp2.dll',
  'uitkforksp2.controls.Runtime.dll',
  'netstandard.dll'
)

$argsList = @(
  '/nologo',
  '/target:library',
  '/langversion:preview',
  '/nullable:enable',
  '/unsafe',
  '/optimize+'
)
$argsList += '/out:' + $out
$argsList += $refs | ForEach-Object { '/reference:' + (Join-Path $managed $_) }
$original = Get-ChildItem (Join-Path $full 'Original') -Recurse -Filter *.cs |
  Where-Object { $_.FullName -ne (Join-Path $full 'Original\CommNextPlugin.cs') } |
  ForEach-Object { $_.FullName }
$redux = Get-ChildItem (Join-Path $full 'ReduxCore') -Filter *.cs |
  Where-Object { $_.Name -notin @(
    'Data_NextModulator.cs',
    'Data_NextRelay.cs',
    'ModulatorKind.cs',
    'Module_NextModulator.cs',
    'Module_NextRelay.cs',
    'PartComponentModule_NextModulator.cs',
    'PartComponentModule_NextRelay.cs'
  ) } |
  ForEach-Object { $_.FullName }
$compat = Get-ChildItem (Join-Path $full 'Compat') -Filter *.cs | ForEach-Object { $_.FullName }
$unityRuntime = Get-ChildItem (Join-Path $root 'src\CommNext.Unity\CommNext.Unity\Assets\Runtime') -Recurse -Filter *.cs |
  Where-Object { $_.Name -notin @('ExampleScript.cs','SampleLine.cs','DebugHDRColor.cs') } |
  ForEach-Object { $_.FullName }

$argsList += $original
$argsList += $redux
$argsList += $compat
$argsList += $unityRuntime

& $csc @argsList
exit $LASTEXITCODE
