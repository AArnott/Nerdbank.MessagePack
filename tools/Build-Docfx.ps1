#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Builds and optionally hosts the docfx site.
.PARAMETER Serve
    A switch to serve the docfx site after building.
.PARAMETER WarningsAsErrors
    A switch to treat warnings as errors during the build.
.PARAMETER DisableGitFeatures
    A switch to disable Git features during the build.
#>
[CmdletBinding()]
Param(
    [switch]$Serve,
    [switch]$WarningsAsErrors,
    [switch]$DisableGitFeatures
)


$env:DocFx = 'true' # Workaround https://github.com/dotnet/docfx/issues/10808
try {
    $docfxArgs = @()

    if ($Serve) {
        $docfxArgs += '--serve'
    }

    if ($WarningsAsErrors) {
        $docfxArgs += '--warningsAsErrors'
    }

    if ($DisableGitFeatures) {
        $docfxArgs += '--disableGitFeatures'
    }

    dotnet docfx $PSScriptRoot/../docfx/docfx.json @docfxArgs
}
finally {
    Remove-Item env:DocFx
}
