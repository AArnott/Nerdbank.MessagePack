<#
.SYNOPSIS
    Signs DLLs and NuGet packages with a certificate in Azure Key Vault.
.DESCRIPTION
    Finds every .nupkg file directly under Path, extracts the packages to a temporary directory,
    and Authenticode-signs every contained DLL with AzureSignTool. It then repacks the packages
    and signs them with NuGetKeyVaultSignTool. All signatures use SHA-256 and an RFC 3161
    timestamp.

    Authentication uses a short-lived Key Vault access token obtained from Azure CLI. For local
    use, run az login first. In GitHub Actions, authenticate with azure/login and OpenID Connect
    before invoking this script. The original packages are replaced only after every DLL and
    package has been signed and verified successfully.
.PARAMETER Path
    The directory containing the .nupkg files to sign.
.PARAMETER KeyVaultUrl
    The URL of the Azure Key Vault containing the signing certificate.
.PARAMETER CertificateName
    The name of the signing certificate in Azure Key Vault.
.PARAMETER TimestampServer
    The RFC 3161 timestamp server used for DLL and package signatures.
.PARAMETER AzureSignToolPath
    An optional path to AzureSignTool.exe. The repository-local .NET tool is used when omitted.
.PARAMETER NuGetKeyVaultSignToolPath
    An optional path to NuGetKeyVaultSignTool.exe. The repository-local .NET tool is used when
    omitted.
.PARAMETER DotNetPath
    The path to dotnet.exe. When omitted, the script searches PATH.
.PARAMETER AzureCliPath
    The path to az.cmd. When omitted, the script searches PATH.
.EXAMPLE
    az login
    .\tools\Sign-NuGetPackages.ps1 -Path .\bin\Packages\Release `
        -KeyVaultUrl https://my-vault.vault.azure.net/ `
        -CertificateName code-signing

    Uses the current Azure CLI login to sign all packages in the directory.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
Param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateNotNullOrEmpty()]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [uri]$KeyVaultUrl,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$CertificateName,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [uri]$TimestampServer = 'https://timestamp.sectigo.com',

    [Parameter()]
    [string]$AzureSignToolPath,

    [Parameter()]
    [string]$NuGetKeyVaultSignToolPath,

    [Parameter()]
    [string]$DotNetPath,

    [Parameter()]
    [string]$AzureCliPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

function Resolve-Executable {
    Param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter()]
        [string]$ExplicitPath
    )

    if ($ExplicitPath) {
        if (-not (Test-Path -LiteralPath $ExplicitPath -PathType Leaf)) {
            throw "$Name was not found at '$ExplicitPath'."
        }

        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $command = Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($command) {
        return $command.Path
    }

    return $null
}

function New-ToolCommand {
    Param(
        [Parameter(Mandatory = $true)]
        [string]$ToolName,

        [Parameter()]
        [string]$ExplicitPath,

        [Parameter(Mandatory = $true)]
        [string]$ResolvedDotNetPath
    )

    if ($ExplicitPath) {
        $resolvedPath = Resolve-Executable -Name $ToolName -ExplicitPath $ExplicitPath
        return @{
            FilePath = $resolvedPath
            Prefix = @()
        }
    }

    return @{
        FilePath = $ResolvedDotNetPath
        Prefix = @('tool', 'run', $ToolName, '--')
    }
}

function Invoke-NativeTool {
    Param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Tool,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $nativeArguments = @($Tool.Prefix) + $Arguments
    & $Tool.FilePath @nativeArguments
    if ($LASTEXITCODE -ne 0) {
        throw "'$($Tool.FilePath)' failed with exit code $LASTEXITCODE."
    }
}

function Get-KeyVaultArguments {
    return @(
        '--azure-key-vault-url', $KeyVaultUrl.AbsoluteUri
        '--azure-key-vault-certificate', $CertificateName
        '--azure-key-vault-accesstoken', $script:keyVaultAccessToken
    )
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'Authenticode signing with AzureSignTool requires Windows.'
}

$packageDirectory = Get-Item -LiteralPath $Path -ErrorAction Stop
if (-not $packageDirectory.PSIsContainer) {
    throw "Path must identify a directory: '$Path'."
}

$packages = @(Get-ChildItem -LiteralPath $packageDirectory.FullName -Filter '*.nupkg' -File |
    Sort-Object -Property Name)
if ($packages.Count -eq 0) {
    throw "No .nupkg files were found in '$($packageDirectory.FullName)'."
}

$targetDescription = "$($packages.Count) package(s) in '$($packageDirectory.FullName)'"
if (-not $PSCmdlet.ShouldProcess($targetDescription, 'Sign package DLLs and NuGet packages')) {
    return
}

$resolvedDotNetPath = Resolve-Executable -Name 'dotnet.exe' -ExplicitPath $DotNetPath
if (-not $resolvedDotNetPath) {
    throw 'dotnet.exe was not found. Install the .NET SDK or pass -DotNetPath.'
}

$resolvedAzureCliPath = Resolve-Executable -Name 'az.cmd' -ExplicitPath $AzureCliPath
if (-not $resolvedAzureCliPath) {
    throw 'Azure CLI was not found. Install it or pass -AzureCliPath.'
}

$script:keyVaultAccessToken = (& $resolvedAzureCliPath account get-access-token `
    --resource 'https://vault.azure.net' `
    --query 'accessToken' `
    --output 'tsv')
if (($LASTEXITCODE -ne 0) -or -not $script:keyVaultAccessToken) {
    throw "Unable to acquire an Azure Key Vault access token. Run 'az login' and try again."
}

$azureSignTool = New-ToolCommand `
    -ToolName 'azuresigntool' `
    -ExplicitPath $AzureSignToolPath `
    -ResolvedDotNetPath $resolvedDotNetPath
$nugetSignTool = New-ToolCommand `
    -ToolName 'NuGetKeyVaultSignTool' `
    -ExplicitPath $NuGetKeyVaultSignToolPath `
    -ResolvedDotNetPath $resolvedDotNetPath

Add-Type -AssemblyName System.IO.Compression.FileSystem

$workspace = Join-Path $packageDirectory.FullName ".signing-$([Guid]::NewGuid().ToString('N'))"
$workingPackages = New-Object System.Collections.Generic.List[System.IO.FileInfo]
try {
    New-Item -ItemType Directory -Path $workspace | Out-Null

    for ($i = 0; $i -lt $packages.Count; $i++) {
        $extractPath = Join-Path $workspace "content-$i"
        New-Item -ItemType Directory -Path $extractPath | Out-Null
        [System.IO.Compression.ZipFile]::ExtractToDirectory($packages[$i].FullName, $extractPath)
        $signaturePath = Join-Path $extractPath '.signature.p7s'
        if (Test-Path -LiteralPath $signaturePath -PathType Leaf) {
            Remove-Item -LiteralPath $signaturePath -Force
        }

        $workingPackagePath = Join-Path $workspace $packages[$i].Name
        $workingPackages.Add([System.IO.FileInfo]$workingPackagePath)
    }

    $dlls = @(Get-ChildItem -LiteralPath $workspace -Filter '*.dll' -File -Recurse |
        Sort-Object -Property FullName)
    if ($dlls.Count -gt 0) {
        $fileListPath = Join-Path $workspace 'dlls-to-sign.txt'
        [System.IO.File]::WriteAllLines(
            $fileListPath,
            [string[]]@($dlls | ForEach-Object { $_.FullName }))

        $signArguments = @(
            'sign'
        ) + (Get-KeyVaultArguments) + @(
            '--file-digest', 'sha256'
            '--timestamp-rfc3161', $TimestampServer.AbsoluteUri
            '--timestamp-digest', 'sha256'
            '--max-degree-of-parallelism', '1'
            '--input-file-list', $fileListPath
            '--quiet'
        )

        Write-Host "Authenticode-signing $($dlls.Count) DLL(s)..." -ForegroundColor Cyan
        Invoke-NativeTool -Tool $azureSignTool -Arguments $signArguments

        $invalidSignatures = @($dlls | Where-Object {
            (Get-AuthenticodeSignature -LiteralPath $_.FullName).Status -ne 'Valid'
        })
        if ($invalidSignatures.Count -gt 0) {
            $invalidPaths = $invalidSignatures.FullName -join "', '"
            throw "Authenticode signature verification failed for '$invalidPaths'."
        }
    } else {
        Write-Warning 'The packages contain no DLLs; only the packages will be signed.'
    }

    for ($i = 0; $i -lt $packages.Count; $i++) {
        $extractPath = Join-Path $workspace "content-$i"
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $extractPath,
            $workingPackages[$i].FullName,
            [System.IO.Compression.CompressionLevel]::Optimal,
            $false)
    }

    Write-Host "Signing $($packages.Count) NuGet package(s)..." -ForegroundColor Cyan
    foreach ($workingPackage in $workingPackages) {
        $signArguments = @(
            'sign'
            $workingPackage.FullName
        ) + (Get-KeyVaultArguments) + @(
            '--file-digest', 'sha256'
            '--timestamp-rfc3161', $TimestampServer.AbsoluteUri
            '--timestamp-digest', 'sha256'
            '--force'
        )
        Invoke-NativeTool -Tool $nugetSignTool -Arguments $signArguments
        Invoke-NativeTool -Tool $nugetSignTool -Arguments @('verify', $workingPackage.FullName)
    }

    for ($i = 0; $i -lt $packages.Count; $i++) {
        $backupPath = Join-Path $workspace "unsigned-$i.nupkg"
        [System.IO.File]::Replace(
            $workingPackages[$i].FullName,
            $packages[$i].FullName,
            $backupPath)
        Remove-Item -LiteralPath $backupPath -Force
        Write-Host "Signed '$($packages[$i].Name)'." -ForegroundColor Green
    }
} finally {
    $script:keyVaultAccessToken = $null

    if (Test-Path -LiteralPath $workspace) {
        Remove-Item -LiteralPath $workspace -Recurse -Force
    }
}
