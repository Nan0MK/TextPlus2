$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$release = "26_1.0.0"
$project = Join-Path $root "csharp\TextPlus2.csproj"
$out = Join-Path $root "releases\$release"

function Publish-Rid([string]$rid) {
    $dest = Join-Path $out $rid
    if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
    dotnet publish $project -c Release -r $rid --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=none `
        -p:DebugSymbols=false `
        -o $dest
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Publish-Rid "win-x64"
Publish-Rid "linux-x64"

$notes = @"
# TextPlus2 $release

Self-contained builds. They do not need a separate .NET install.

- ``win-x64/TextPlus2.exe`` runs on 64-bit Windows.
- ``linux-x64/TextPlus2`` runs on 64-bit Linux. Mark it executable if the file system did not: ``chmod +x TextPlus2``.

Pass a file to open it, or a folder to root the file tree there.

This cut has New, Open, Open Folder, Save, Save As, Close, undo and redo, cut, copy, paste, select all, find, line numbers, zoom, light and dark, syntax highlighting, document tabs, a hideable file tree, word wrap, and the app icon. A click in the tree opens a file. Ctrl+Tab switches documents. Find and replace, language servers, and a rendered markdown preview are not in this release. Theme, zoom, the tree, follow-open-file, and word wrap last for the session only.
"@
Set-Content -Path (Join-Path $out "NOTES.md") -Value $notes -Encoding utf8
Write-Output "Release written to $out"
