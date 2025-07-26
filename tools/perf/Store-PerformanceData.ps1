#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stores performance benchmark data and AOT file size to Azure Table Storage
.DESCRIPTION
    This script stores performance data to Azure Table Storage for CI builds on the main branch.
    Data includes benchmark results and AotNativeConsole file size metrics.
.PARAMETER StorageAccount
    The Azure Storage Account name
.PARAMETER TableName
    The Azure Table name to store data
.PARAMETER BenchmarkResultsPath
    Path to the BenchmarkDotNet JSON results file
.PARAMETER AotFileSizeMB
    Size of the AotNativeConsole file in MB
.PARAMETER CloudBuildNumber
    The cloud build number from nbgv
.PARAMETER Version
    The version from nbgv
.PARAMETER VersionMajor
    The major version number
.PARAMETER VersionMinor
    The minor version number
.PARAMETER BuildNumber
    The build number
.PARAMETER CommitId
    The commit SHA
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$StorageAccount,

    [Parameter(Mandatory)]
    [string]$TableName,

    [Parameter(Mandatory)]
    [string]$BenchmarkResultsPath,

    [Parameter(Mandatory)]
    [double]$AotFileSizeMB,

    [Parameter(Mandatory)]
    [string]$CloudBuildNumber,

    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [int]$VersionMajor,

    [Parameter(Mandatory)]
    [int]$VersionMinor,

    [Parameter(Mandatory)]
    [int]$BuildNumber,

    [Parameter(Mandatory)]
    [string]$CommitId
)

try {
    # Check and install required modules if not available
    $requiredModules = @('Az.Storage', 'Az.Resources', 'Az.Accounts', 'AzTable')
    foreach ($module in $requiredModules) {
        if (-not (Get-Module -ListAvailable -Name $module)) {
            Write-Host "📦 Installing module: $module"
            Install-Module -Name $module -Force -Scope CurrentUser -Repository PSGallery
        }
        Import-Module $module -Force
    }

    Write-Host "🔐 Authenticating to Azure using Managed Identity..."
    # Connect using managed identity
    Connect-AzAccount -Identity

    Write-Host "🏪 Connecting to storage account: $StorageAccount"
    $storageContext = New-AzStorageContext -StorageAccountName $StorageAccount -UseConnectedAccount

    # Get or create table
    $table = Get-AzStorageTable -Name $TableName -Context $storageContext -ErrorAction SilentlyContinue
    if (-not $table) {
        Write-Host "📊 Creating table: $TableName"
        $table = New-AzStorageTable -Name $TableName -Context $storageContext
    }

    Write-Host "📈 Reading benchmark results from: $BenchmarkResultsPath"
    if (-not (Test-Path $BenchmarkResultsPath)) {
        throw "Benchmark results file not found: $BenchmarkResultsPath"
    }

    $benchmarkData = Get-Content $BenchmarkResultsPath -Raw | ConvertFrom-Json

    # Create table entity for AOT file size
    $timestamp = Get-Date
    $partitionKey = "AotFileSize"
    $rowKey = $CloudBuildNumber

    Write-Host "💾 Storing AOT file size data..."
    $aotEntity = @{
        Timestamp = $timestamp
        CommitId = $CommitId
        CloudBuildNumber = $CloudBuildNumber
        Version = $Version
        VersionMajor = $VersionMajor
        VersionMinor = $VersionMinor
        BuildNumber = $BuildNumber
        FileSizeMB = $AotFileSizeMB
    }

    Add-AzTableRow -Table $table.CloudTable -PartitionKey $partitionKey -RowKey $rowKey -property $aotEntity

    # Store benchmark data for each tracked benchmark
    $trackedBenchmarks = @(
        'SimplePoco.DeserializeMapInit',
        'SimplePoco.DeserializeMap',
        'SimplePoco.SerializeMap',
        'SimplePoco.SerializeAsArray',
        'SimplePoco.DeserializeAsArray'
    )

    Write-Host "📊 Storing benchmark data..."
    foreach ($benchmark in $benchmarkData.Benchmarks) {
        $methodName = $benchmark.MethodTitle
        if ($methodName -in $trackedBenchmarks) {
            $partitionKey = "Benchmark"
            $rowKey = "$CloudBuildNumber-$methodName"

            $benchmarkEntity = @{
                Timestamp = $timestamp
                CommitId = $CommitId
                CloudBuildNumber = $CloudBuildNumber
                Version = $Version
                VersionMajor = $VersionMajor
                VersionMinor = $VersionMinor
                BuildNumber = $BuildNumber
                MethodName = $methodName
                Mean = $benchmark.Statistics.Mean
                StdDev = $benchmark.Statistics.StandardDeviation
                Min = $benchmark.Statistics.Min
                Max = $benchmark.Statistics.Max
                Allocated = $benchmark.Memory.Allocated
            }

            Add-AzTableRow -Table $table.CloudTable -PartitionKey $partitionKey -RowKey $rowKey -property $benchmarkEntity
            Write-Host "  ✅ Stored: $methodName (Mean: $($benchmark.Statistics.Mean) ns)"
        }
    }

    Write-Host "✅ Successfully stored performance data to Azure Table Storage"
}
catch {
    Write-Error "❌ Failed to store performance data: $($_.Exception.Message)"
    throw
}
