#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the same gates CI runs, then stages a build the host can load.

.DESCRIPTION
    One command, so that "it passes here" and "it passes in CI" are the same check rather than
    two. Without it the difference is discovered after pushing, by somebody waiting.

    The staging step is not convenience. The host reloads a development plugin as soon as its
    main assembly changes on disk, and this plugin ships more than one assembly. Pointed at the
    raw build output, a reload can fire while the main assembly is new and Gearbook.Core.dll is
    still old or half written, which loads the plugin against a mismatched dependency and takes
    the game down. So the publish goes into a staging folder and the main assembly is copied
    last, when everything it depends on is already complete.

.PARAMETER Configuration
    Debug or Release. Defaults to Release, which is what CI builds.

.PARAMETER SkipChecks
    Skips the format check and the tests. For a quick inner loop only; never before a commit.

.PARAMETER NoStage
    Builds and checks without producing the staging folder.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$SkipChecks,

    [switch]$NoStage
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = $PSScriptRoot
$solution = Join-Path $root 'Gearbook.slnx'
$pluginProject = Join-Path $root 'src/Gearbook/Gearbook.csproj'
$stagingDir = Join-Path $root 'dist/Gearbook'
$mainAssembly = 'Gearbook.dll'

function Write-Step {
    param([string]$Text)
    Write-Host ''
    Write-Host "==> $Text" -ForegroundColor Cyan
}

function Invoke-Checked {
    param([string]$Description, [scriptblock]$Command)
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

# The developer-tools switch is invisible in the output folder and very visible in the game, so
# say which of the two builds this is before anything else scrolls past.
$devToolsFile = Join-Path $root 'Directory.Build.local.props'
$withDevTools = Test-Path $devToolsFile

Write-Host ''
if ($withDevTools) {
    Write-Host 'Building WITH developer tools (Directory.Build.local.props is present).' -ForegroundColor Yellow
    Write-Host 'That file is git-ignored and CI fails if it is ever committed.' -ForegroundColor Yellow
}
else {
    Write-Host 'Building WITHOUT developer tools. This is what a release contains.' -ForegroundColor Green
}

Write-Step "Restoring ($Configuration)"
Invoke-Checked 'Restore' { dotnet restore $solution }

Write-Step "Building ($Configuration)"
Invoke-Checked 'Build' { dotnet build $solution --configuration $Configuration --no-restore }

if (-not $SkipChecks) {
    Write-Step 'Running tests'
    Invoke-Checked 'Tests' { dotnet test $solution --configuration $Configuration --no-build }

    Write-Step 'Checking formatting'
    # Exits non-zero when files would have been reformatted, which is what makes it usable as a
    # gate without extra tooling.
    Invoke-Checked 'Format check' { dotnet format $solution --verify-no-changes }
}
else {
    Write-Host ''
    Write-Host 'Checks skipped. Do not commit on the strength of this run.' -ForegroundColor Yellow
}

if ($NoStage) {
    Write-Host ''
    Write-Host 'Done. Staging skipped.' -ForegroundColor Green
    return
}

Write-Step 'Staging a loadable build'

# The staging folder is written into rather than emptied first. While the game is running with
# this plugin loaded, the host holds the assemblies open: they can be overwritten but the folder
# cannot be deleted, so wiping it would fail exactly when a rebuild is most wanted. The publish
# folder below is not held by anything and is emptied normally.
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

$publishDir = Join-Path $root 'dist/.publish'
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Invoke-Checked 'Publish' {
    dotnet publish $pluginProject --configuration $Configuration --no-build --output $publishDir
}

# Everything except the main assembly first.
Get-ChildItem -Path $publishDir -File -Recurse |
    Where-Object { $_.Name -ne $mainAssembly } |
    ForEach-Object {
        $relative = $_.FullName.Substring($publishDir.Length).TrimStart('\', '/')
        $target = Join-Path $stagingDir $relative
        $targetDir = Split-Path $target -Parent
        if (-not (Test-Path $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }
        Copy-Item $_.FullName $target -Force
    }

# The manifest the packager built, not the one in the repository. The host reads it next to the
# assembly to learn what the plugin is, and the built copy is the authoritative one because it
# carries the fields the packager adds: the API level and the assembly version. Publishing does
# not copy it, which is why this line exists rather than being implied by the publish step.
$builtManifest = Join-Path $root "src/Gearbook/bin/$Configuration/Gearbook/Gearbook.json"
if (-not (Test-Path $builtManifest)) {
    throw "The built manifest was not found at $builtManifest. The host would load an assembly it knows nothing about."
}
Copy-Item $builtManifest (Join-Path $stagingDir 'Gearbook.json') -Force

# The icon, because a development build reads it from its own directory rather than from the
# manifest. Without this the local build shows the default picture while the plugin list shows
# the real one, and the two never agree.
$icon = Join-Path $root 'src/Gearbook/images/icon.png'
if (Test-Path $icon) {
    Copy-Item $icon (Join-Path $stagingDir 'icon.png') -Force
}

# The main assembly last. This is the line the whole staging step exists for: the host reloads
# on a change to it, so it must not see it until everything it depends on is already complete.
$mainSource = Join-Path $publishDir $mainAssembly
if (-not (Test-Path $mainSource)) {
    throw "The main assembly $mainAssembly was not found in the publish output."
}

try {
    Copy-Item $mainSource (Join-Path $stagingDir $mainAssembly) -Force
}
catch [System.IO.IOException] {
    # A file lock with no obvious owner, and the natural first guess is an editor or a virus
    # scanner. It is neither: the host has the assembly open. Saying so here saves the twenty
    # minutes it otherwise takes to work out.
    throw "The staged copy of $mainAssembly could not be replaced because something has it open. " +
          "That is almost certainly the game, with this plugin loaded. Unload it in the plugin " +
          "installer or close the client, then run this again. Everything else has already been staged."
}

Write-Host ''
Write-Host 'Done.' -ForegroundColor Green
Write-Host ''
Write-Host 'Register this path as a dev plugin location in Dalamud:' -ForegroundColor Green
Write-Host "  $stagingDir"
Write-Host ''
Write-Host 'Do not point the host at bin/ directly. The reason is in the header of this script.'
