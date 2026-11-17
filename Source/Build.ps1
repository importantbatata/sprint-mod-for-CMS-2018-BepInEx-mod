param([string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Car Mechanic Simulator 2018')
$ErrorActionPreference = 'Stop'
$refs = @("/reference:$GamePath\BepInEx\core\BepInEx.dll", "/reference:$GamePath\BepInEx\core\0Harmony.dll", "/reference:$GamePath\cms2018_Data\Managed\Assembly-CSharp-firstpass.dll")
$refs += @("/reference:$GamePath\cms2018_Data\Managed\mscorlib.dll", "/reference:$GamePath\cms2018_Data\Managed\System.dll", "/reference:$GamePath\cms2018_Data\Managed\System.Core.dll")
$refs += Get-ChildItem -LiteralPath "$GamePath\cms2018_Data\Managed" -Filter 'UnityEngine*.dll' | ForEach-Object { '/reference:' + $_.FullName }
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /noconfig /nostdlib+ /target:library /optimize+ "/out:$PSScriptRoot\SPRINT MOD CMS 2018.dll" $refs "$PSScriptRoot\SPRINT MOD CMS 2018.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
