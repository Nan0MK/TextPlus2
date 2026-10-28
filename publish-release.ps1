# Uploads an already-built local release to GitHub Releases.
# Does not build, commit, or push. releases/ stays gitignored.
#
#   powershell -File publish-release.ps1 -Version 26_1.0.0
#
# Requires a clean master that matches origin/master, and `gh auth status` logged in.
# The upload is about 200 MB and can take 10 to 20 minutes. Wait for the URL.

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

function Find-Gh {
    $cmd = Get-Command gh -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $candidate = Join-Path $env:ProgramFiles "GitHub CLI\gh.exe"
    if (Test-Path $candidate) { return $candidate }
    throw "GitHub CLI is not installed. Install it with: winget install --id GitHub.cli"
}

if (-not $Version) {
    $text = [IO.File]::ReadAllText($project)
    $m = [regex]::Match($text, '<InformationalVersion>([^<]*)</InformationalVersion>')
    if (-not $m.Success) { throw "Pass -Version 26_1.0.0 or set InformationalVersion." }
    $Version = $m.Groups[1].Value.Trim()
}
if ($Version -notmatch '^\d+_\d+\.\d+\.\d+$') {
    throw "Version must look like 26_1.0.0. Got '$Version'."
}

$drop = Join-Path $root "releases\$Version"
$win = Join-Path $drop "win-x64\TextPlus2.exe"
$linux = Join-Path $drop "linux-x64\TextPlus2"
$notesFile = Join-Path $drop "NOTES.md"
foreach ($path in @($win, $linux, $notesFile)) {
    if (-not (Test-Path $path)) { throw "Missing $path. Build it with build-release.ps1 -Version $Version" }
}

Set-Location $root
$dirty = git status --porcelain
if ($dirty) {
    throw "Working tree is not clean. Commit and push source changes first. Do not commit releases/.`n$dirty"
}
$branch = (git rev-parse --abbrev-ref HEAD).Trim()
git fetch origin $branch
if ($LASTEXITCODE -ne 0) { throw "git fetch origin $branch failed." }
$head = (git rev-parse HEAD).Trim()
$remote = (git rev-parse "origin/$branch").Trim()
if ($head -ne $remote) {
    throw "HEAD $head is not origin/$branch $remote. Push the source, then run this again."
}

$gh = Find-Gh
& $gh auth status
if ($LASTEXITCODE -ne 0) {
    throw "gh is not logged in. Run: gh auth login --hostname github.com --git-protocol https --web --skip-ssh-key"
}

& $gh release view $Version --json url 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    throw "GitHub release $Version already exists. Do not upload it again."
}

$notes = [IO.File]::ReadAllText($notesFile)
if ($notes.Length -gt 0 -and [int][char]$notes[0] -eq 0xFEFF) { $notes = $notes.Substring(1) }
$notes = $notes.Replace('win-x64/TextPlus2.exe', 'TextPlus2.exe')
$notes = $notes.Replace('linux-x64/TextPlus2', 'TextPlus2')
$uploadNotes = Join-Path $env:TEMP "textplus2-$Version-notes.md"
$utf8 = New-Object System.Text.UTF8Encoding $false
[IO.File]::WriteAllText($uploadNotes, $notes.Replace("`r`n", "`n"), $utf8)

try {
    & $gh release create $Version $win $linux --target $head --title $Version --notes-file $uploadNotes
    if ($LASTEXITCODE -ne 0) { throw "gh release create failed." }

    $view = & $gh release view $Version --json isDraft,url,assets | ConvertFrom-Json
    $exe = $view.assets | Where-Object { $_.name -eq 'TextPlus2.exe' } | Select-Object -First 1
    $elf = $view.assets | Where-Object { $_.name -eq 'TextPlus2' } | Select-Object -First 1
    $winSize = (Get-Item $win).Length
    $linuxSize = (Get-Item $linux).Length
    if ($view.isDraft) { throw "Release $Version is still a draft." }
    if (-not $exe -or $exe.size -ne $winSize) { throw "Windows asset is missing or the wrong size." }
    if (-not $elf -or $elf.size -ne $linuxSize) { throw "Linux asset is missing or the wrong size." }
    Write-Output $view.url
}
finally {
    if (Test-Path $uploadNotes) { Remove-Item -Force $uploadNotes }
}
