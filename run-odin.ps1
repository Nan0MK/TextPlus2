$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$odinCmd = Get-Command odin -ErrorAction Stop
$dist = Split-Path $odinCmd.Source
$outDir = Join-Path $root "odin"
$out = Join-Path $outDir "textplus2_odin.exe"

& $odinCmd.Source build (Join-Path $outDir "src") "-out:$out" -o:speed "-define:MICROUI_COMMAND_LIST_SIZE=8388608"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item (Join-Path $dist "vendor\sdl3\SDL3.dll") (Join-Path $outDir "SDL3.dll") -Force
& $out @args
exit $LASTEXITCODE
