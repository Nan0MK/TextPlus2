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
#   MSI ProductVersion    26.1.0      (patch and revision share the third field)

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
    # Windows Installer compares only three fields and ignores a fourth.
    # 26_1.0.0 -> 26.1.0, 26_1.0.1 -> 26.1.1, 26_1.2.3 -> 26.1.2003.
    $msiBuild = ([int]$parts[2] * 1000) + [int]$parts[3]
    if ($msiBuild -gt 65535) { throw "MSI build number $msiBuild is above 65535." }
    return [PSCustomObject]@{
        Informational = $informational
        Version       = $nugetVersion
        Assembly      = $assemblyVersion
        File          = $fileVersion
        Msi           = "$($parts[0]).$($parts[1]).$msiBuild"
    }
}

function Get-ReleaseNotes([string]$informational) {
    # Each release lists only the features that version adds.
    switch ($informational) {
        "26_1.0.0" {
            return @"
# TextPlus2 26_1.0.0

- New, Open, Save, Save As, Close, and Open Folder.
- Undo, redo, cut, copy, paste, select all, and find.
- Several files stay open in tabs.
- A file tree. A click opens a file, and Follow Open File can retarget the tree.
- Syntax highlighting, including Markdown colored in the source.
- Line numbers, zoom, and word wrap.
- Light and dark themes.
- The app icon.
"@
        }
        "26_1.1.0" {
            return @"
# TextPlus2 26_1.1.0

- Windows installs from TextPlus2.msi, with a Start menu shortcut.
- The drive list loads without blocking the window.
"@
        }
        "26_1.2.0" {
            return @"
# TextPlus2 26_1.2.0

- Theme, zoom, the file tree, Follow Open File, word wrap, and the editor font are saved.
- Cascadia Mono and IBM Plex Mono can be chosen as the editor font.
- Setup can add TextPlus2 to Open with. It does not change the default app.
"@
        }
        "26_1.3.0" {
            return @"
# TextPlus2 26_1.3.0

- Ctrl+click follows a Markdown link. A web address opens in the browser, a heading link jumps in the file, and a path opens that file. ``[[name]]`` opens the matching Markdown file.
- Ctrl+Left and Ctrl+Right move by word. Ctrl+Up and Ctrl+Down move by paragraph. Left or Right with a selection moves to that end.
- Setup asks for an install folder. The default is Program Files\TextPlus2, and a later install remembers the folder you chose.
- Minimize, maximize, and close are yellow, green, and red. Menus, context menus, and flyouts have a drop shadow.
"@
        }
        default { throw "Add release notes for $informational before building. Notes list only the features that version adds." }
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

function Publish-SelfContained([string]$rid, [string]$dest, [bool]$readyToRun) {
    if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
    $publishArgs = @(
        "publish", $project, "-c", "Release", "-r", $rid, "--self-contained", "true",
        "-p:DebugType=none", "-p:DebugSymbols=false",
        "-p:IncludeSourceRevisionInInformationalVersion=false",
        "-o", $dest, "--nologo"
    )
    if ($readyToRun) {
        $publishArgs += @(
            "-p:PublishSingleFile=false",
            "-p:PublishReadyToRun=true",
            "-p:PublishReadyToRunComposite=true"
        )
    }
    else {
        $publishArgs += @(
            "-p:PublishSingleFile=true",
            "-p:IncludeNativeLibrariesForSelfExtract=true"
        )
    }
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

function Assert-Msi([string]$path, [string]$version) {
    $installer = New-Object -ComObject WindowsInstaller.Installer
    $db = $installer.OpenDatabase($path, 0)
    $view = $db.OpenView("SELECT ``Value`` FROM ``Property`` WHERE ``Property``=?")
    $record = $installer.CreateRecord(1)
    $record.StringData(1) = "ProductName"
    $view.Execute($record) | Out-Null
    $row = $view.Fetch()
    if (-not $row) { throw "MSI is missing ProductName." }
    $product = $row.StringData(1).Trim()
    $record.StringData(1) = "ProductVersion"
    $view.Execute($record) | Out-Null
    $row = $view.Fetch()
    if (-not $row) { throw "MSI is missing ProductVersion." }
    $productVersion = $row.StringData(1).Trim()
    if ($product -ne "TextPlus2") { throw "MSI product name is '$product'." }
    if ($productVersion -ne $version) { throw "MSI version is $productVersion, expected $version." }

    $files = $db.OpenView("SELECT ``FileName`` FROM ``File``")
    $files.Execute()
    $names = @{}
    while ($row = $files.Fetch()) {
        $fileName = $row.StringData(1)
        if ($fileName -match '\|(.+)$') { $fileName = $Matches[1] }
        $names[$fileName] = $true
    }
    foreach ($need in @("TextPlus2.exe", "TextPlus2.r2r.dll", "libSkiaSharp.dll", "TextPlus2.toml")) {
        if (-not $names.ContainsKey($need)) { throw "MSI is missing $need." }
    }
}

$winDir = Join-Path $out "win-x64"
$msi = Join-Path $out "TextPlus2.msi"
if (Test-Path $msi) { Remove-Item $msi -Force }
Publish-SelfContained "win-x64" $winDir $true

$msiStage = Join-Path $out "msi-build"
if (Test-Path $msiStage) { Remove-Item $msiStage -Recurse -Force }
# WiX incremental builds keep the previous ProductVersion and file set.
foreach ($wixOut in @("obj", "bin")) {
    $wixPath = Join-Path $root "installer\$wixOut"
    if (Test-Path $wixPath) { Remove-Item $wixPath -Recurse -Force }
}
$publishDir = (Resolve-Path $winDir).Path
& dotnet build (Join-Path $root "installer\TextPlus2.wixproj") -c Release -t:Rebuild --nologo `
    -p:PublishDir="$publishDir" `
    -p:MsiVersion="$($numbers.Msi)" `
    -o $msiStage
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$stagedMsi = Join-Path $msiStage "TextPlus2.msi"
if (-not (Test-Path $stagedMsi)) { throw "WiX did not write $stagedMsi." }
Copy-Item $stagedMsi $msi -Force
Remove-Item $msiStage -Recurse -Force
Assert-Msi $msi $numbers.Msi

Publish-SelfContained "linux-x64" (Join-Path $out "linux-x64") $false

$win = Join-Path $out "win-x64\TextPlus2.exe"
$linux = Join-Path $out "linux-x64\TextPlus2"
$winInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($win)
if ($winInfo.FileVersion -ne $numbers.File -or $winInfo.ProductVersion -ne $numbers.Informational) {
    throw "Windows file version is $($winInfo.FileVersion) / $($winInfo.ProductVersion), expected $($numbers.File) / $($numbers.Informational)."
}
$magic = [IO.File]::ReadAllBytes($linux)[0..3]
$magicText = ($magic | ForEach-Object { '{0:X2}' -f $_ }) -join ' '
if ($magicText -ne '7F 45 4C 46') { throw "Linux build is not an ELF file ($magicText)." }
foreach ($config in @(
    (Join-Path $out "win-x64\TextPlus2.toml"),
    (Join-Path $out "linux-x64\TextPlus2.toml")
)) {
    if (-not (Test-Path $config)) { throw "Missing $config." }
}

$notes = Get-ReleaseNotes $numbers.Informational
$utf8 = New-Object System.Text.UTF8Encoding $false
[IO.File]::WriteAllText((Join-Path $out "NOTES.md"), $notes.Replace("`r`n", "`n"), $utf8)

Write-Output "Release written to $out"
Write-Output "Windows $($winInfo.FileVersion) $($winInfo.ProductVersion) MSI $((Get-Item $msi).Length) bytes"
Write-Output "Linux ELF $((Get-Item $linux).Length) bytes"
