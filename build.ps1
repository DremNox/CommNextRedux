$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $env:KSP2DIR) { throw 'Define KSP2DIR with the KSP2 Redux root folder.' }
$game = $env:KSP2DIR
$managed = Join-Path $game 'KSP2_x64_Data\Managed'
$csc = Join-Path $project 'tools\roslyn\tasks\net472\csc.exe'
$version = '0.0.2'
$out = Join-Path $project ("CommNextRedux.$version.dll")
$refs = @('Assembly-CSharp.dll','ReduxLib.dll','SpaceWarp2.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.InputLegacyModule.dll','netstandard.dll')
$argsList = @('/nologo','/target:library','/langversion:latest','/optimize+')
$argsList += '/out:' + $out
$argsList += $refs | ForEach-Object { '/reference:' + (Join-Path $managed $_) }
$argsList += Join-Path $project 'src\CommNextReduxPlugin.cs'
& $csc @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Output "BUILD OK: $out"