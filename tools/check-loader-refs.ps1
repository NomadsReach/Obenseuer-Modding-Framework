param([string]$Cecil = 'C:\Program Files (x86)\Steam\steamapps\common\Obenseuer\BepInEx\core\Mono.Cecil.dll', [string[]]$Be6 = @(), [string[]]$Be5 = @(), [string[]]$Neutral = @())
$ErrorActionPreference = 'Stop'
Add-Type -Path $Cecil
$Be6 = @($Be6 | ForEach-Object { $_ -split "," } | Where-Object { $_ })
$Be5 = @($Be5 | ForEach-Object { $_ -split "," } | Where-Object { $_ })
$Neutral = @($Neutral | ForEach-Object { $_ -split "," } | Where-Object { $_ })
$bad = 0
function Check([string[]]$files, [string[]]$forbidden, [string]$label) {
  foreach ($f in $files) {
    $refs = @([Mono.Cecil.AssemblyDefinition]::ReadAssembly($f).MainModule.AssemblyReferences | ForEach-Object { $_.Name })
    $hit = @($refs | Where-Object { $name = $_; @($forbidden | Where-Object { $name -like $_ }).Count -gt 0 })
    if ($hit.Count) { "FAIL $label $f references $($hit -join ',')"; $script:bad = 1 } else { "ok   $label $f" }
  }
}
Check $Be6 @('BepInEx') 'BepInEx6'
Check $Be5 @('BepInEx.Core','BepInEx.Unity.Mono','BepInEx.Unity.Common','BepInEx.Preloader.Core','SemanticVersioning') 'BepInEx5'
Check $Neutral @('BepInEx*','SemanticVersioning') 'Neutral'
exit $bad
