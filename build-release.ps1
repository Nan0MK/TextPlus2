# Builds one local TextPlus2 release into releases/<version>/.
# Does not upload anywhere and does not commit.
#
#   powershell -File build-release.ps1 -Version 26_1.0.0
#
# With no -Version, rebuilds the InformationalVersion already in the csproj.
# Passing -Version updates these four properties, then publishes:
#   InformationalVersion  26_1.0.0
#   Version               26.1.0      (trailing .0 stripped)
#   AssemblyVersion       26.1.0.0
#   FileVersion           26.1.0.0

param(
    [Parameter(Position = 0)]
    [string]$Version
)

$ErrorActionPreference = "Stop"
if (Get-Variable PSNativeCommandUseErrorActionPreference -Scope Global -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}
$root = $PSScriptRoot
$project = Join-Path $root "csharp\TextPlus2.csproj"

function Get-ReleaseNumbers([string]$informational) {
    if ($informational -notmatch '^\d+_\d+\.\d+\.\d+$') {
        throw "Version must look like 26_1.0.0. Got '$informational'."
    }
    $fileVersion = $informational.Replace('_', '.')
    $parts = $fileVersion.Split('.')
    if ($parts.Count -ne 4) { throw "Could not make a 4-part file version from '$informational'." }
    $assemblyVersion = $fileVersion
    $nugetVersion = if ($parts[3] -eq '0') { ($parts[0..2] -join '.') } else { $fileVersion }
    return [PSCustomObject]@{
        Informational = $informational
        Version       = $nugetVersion
        Assembly      = $assemblyVersion
        File          = $fileVersion
    }
}

function Read-ProjectInformational {
    $text = [IO.File]::ReadAllText($project)
    $m = [regex]::Match($text, '<InformationalVersion>([^<]*)</InformationalVersion>')
    if (-not $m.Success) { throw "InformationalVersion is missing from $project" }
    return $m.Groups[1].Value.Trim()
}

function Set-ProjectVersions($numbers) {
    $text = [IO.File]::ReadAllText($project)
    $pairs = @{
        Version              = $numbers.Version
        AssemblyVersion      = $numbers.Assembly
        FileVersion          = $numbers.File
        InformationalVersion = $numbers.Informational
    }
    foreach ($name in $pairs.Keys) {
        $pattern = "(<$name>)[^<]*(</$name>)"
        if ($text -notmatch $pattern) { throw "$name is missing from $project" }
        $text = [regex]::Replace($text, $pattern, ('${1}' + $pairs[$name] + '${2}'), 1)
    }
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [IO.File]::WriteAllText($project, $text, $utf8)
}

$informational = if ($Version) { $Version } else { Read-ProjectInformational }
$numbers = Get-ReleaseNumbers $informational
if ($Version) { Set-ProjectVersions $numbers }

$out = Join-Path $root "releases\$($numbers.Informational)"

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

$win = Join-Path $out "win-x64\TextPlus2.exe"
$linux = Join-Path $out "linux-x64\TextPlus2"
$winInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($win)
if ($winInfo.FileVersion -ne $numbers.File -or $winInfo.ProductVersion -ne $numbers.Informational) {
    throw "Windows file version is $($winInfo.FileVersion) / $($winInfo.ProductVersion), expected $($numbers.File) / $($numbers.Informational)."
}
$magic = [IO.File]::ReadAllBytes($linux)[0..3]
$magicText = ($magic | ForEach-Object { '{0:X2}' -f $_ }) -join ' '
if ($magicText -ne '7F 45 4C 46') { throw "Linux build is not an ELF file ($magicText)." }

$notes = @"
# TextPlus2 $($numbers.Informational)

Self-contained builds. They do not need a separate .NET install.

- ``win-x64/TextPlus2.exe`` runs on 64-bit Windows.
- ``linux-x64/TextPlus2`` runs on 64-bit Linux. Mark it executable if the file system did not: ``chmod +x TextPlus2``.

Pass a file to open it, or a folder to root the file tree there.

This cut has New, Open, Open Folder, Save, Save As, Close, undo and redo, cut, copy, paste, select all, find, line numbers, zoom, light and dark, syntax highlighting, document tabs, a hideable file tree, word wrap, and the app icon. A click in the tree opens a file. Ctrl+Tab switches documents. Replace, language servers, and a rendered markdown preview are not in this release. Theme, zoom, the tree, follow-open-file, and word wrap last for the session only.
"@
$utf8 = New-Object System.Text.UTF8Encoding $false
[IO.File]::WriteAllText((Join-Path $out "NOTES.md"), $notes.Replace("`r`n", "`n"), $utf8)

Write-Output "Release written to $out"
Write-Output "Windows $($winInfo.FileVersion) $($winInfo.ProductVersion) $((Get-Item $win).Length) bytes"
Write-Output "Linux ELF $((Get-Item $linux).Length) bytes"
