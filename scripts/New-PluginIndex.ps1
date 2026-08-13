#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the single-entry plugin index from the manifest inside the built package.

.DESCRIPTION
    Starts from the built manifest and adds the download links to it, rather than composing an
    entry from scratch. Every field the packager produced then carries through unchanged,
    including the API level, which is what decides whether the plugin is visible at all.

    The result is a JSON array even with a single entry in it. A bare object is rejected by the
    client.

    Never committed. A committed copy names the version that was current when it was written, so
    it can only ever be stale.

.PARAMETER ManifestPath
    The manifest the packager produced, not the one in the repository. The built one is the
    authoritative copy because it is what the index and the installer read.

.PARAMETER Repository
    owner/name, passed in by the workflow rather than known by the script.

.PARAMETER Tag
    The version tag being released.

.PARAMETER OutputPath
    Where to write the index file.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ManifestPath,
    [Parameter(Mandatory = $true)][string]$Repository,
    [Parameter(Mandatory = $true)][string]$Tag,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Test-Path $ManifestPath)) {
    throw "The built manifest was not found at $ManifestPath. Nothing can be published from a build that did not produce one."
}

$manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json

# The download address points at the "latest release" redirect rather than at a tagged path, so
# it never changes between releases and an installed copy keeps working.
$download = "https://github.com/$Repository/releases/latest/download/latest.zip"

$manifest | Add-Member -NotePropertyName 'DownloadLinkInstall' -NotePropertyValue $download -Force
$manifest | Add-Member -NotePropertyName 'DownloadLinkUpdate' -NotePropertyValue $download -Force
$manifest | Add-Member -NotePropertyName 'DownloadLinkTesting' -NotePropertyValue $download -Force

$outputDirectory = Split-Path $OutputPath -Parent
if ($outputDirectory -and -not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

# An array, even for one entry.
$json = ConvertTo-Json -InputObject @($manifest) -Depth 10
[System.IO.File]::WriteAllText($OutputPath, $json)

Write-Host "Wrote the index for $Tag to $OutputPath."
