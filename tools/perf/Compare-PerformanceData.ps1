#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Retrieves historical performance data and generates comparison for PR builds
.DESCRIPTION
    This script retrieves the last 10 CI builds from Azure Table Storage and compares
    current PR performance against the historical trend. Generates graphs and reports.
.PARAMETER StorageAccount
    The Azure Storage Account name
.PARAMETER TableName
    The Azure Table name containing historical data
.PARAMETER BenchmarkResultsPath
    Path to the current BenchmarkDotNet JSON results file
.PARAMETER AotFileSizeMB
    Current AotNativeConsole file size in MB
.PARAMETER OutputPath
    Directory to write comparison reports and graphs
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
    
    [Parameter()]
    [string]$OutputPath = "./perf-comparison"
)

try {
    # Check and install required modules if not available
    $requiredModules = @('Az.Storage', 'Az.Accounts')
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
    
    # Get table
    $table = Get-AzStorageTable -Name $TableName -Context $storageContext
    if (-not $table) {
        throw "Table not found: $TableName"
    }
    
    # Ensure output directory exists
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    }
    
    Write-Host "📊 Retrieving historical AOT file size data..."
    # Get last 10 AOT file size entries
    $aotQuery = "PartitionKey eq 'AotFileSize'"
    $aotEntities = Get-AzTableRow -Table $table.CloudTable -CustomFilter $aotQuery | 
                   Sort-Object BuildNumber -Descending | 
                   Select-Object -First 10 |
                   Sort-Object BuildNumber
    
    if ($aotEntities.Count -eq 0) {
        Write-Warning "⚠️ No historical AOT file size data found"
        $aotRegression = $false
    } else {
        Write-Host "📈 Analyzing AOT file size trend..."
        $aotSizes = $aotEntities | ForEach-Object { [double]$_.FileSizeMB }
        $aotMean = ($aotSizes | Measure-Object -Average).Average
        $aotStdDev = [Math]::Sqrt(($aotSizes | ForEach-Object { [Math]::Pow($_ - $aotMean, 2) } | Measure-Object -Average).Average)
        
        # Check if current size is beyond 2 standard deviations (regression)
        $aotRegression = $AotFileSizeMB -gt ($aotMean + 2 * $aotStdDev)
        
        Write-Host "  📏 Historical AOT size - Mean: $([math]::Round($aotMean, 2)) MB, StdDev: $([math]::Round($aotStdDev, 2)) MB"
        Write-Host "  📏 Current AOT size: $AotFileSizeMB MB"
        Write-Host "  📏 Regression threshold: $([math]::Round($aotMean + 2 * $aotStdDev, 2)) MB"
        
        if ($aotRegression) {
            Write-Warning "⚠️ AOT file size regression detected!"
        } else {
            Write-Host "✅ AOT file size within acceptable range"
        }
    }
    
    # Track benchmark regressions
    $benchmarkRegressions = @()
    $trackedBenchmarks = @(
        'SimplePoco.DeserializeMapInit',
        'SimplePoco.DeserializeMap', 
        'SimplePoco.SerializeMap',
        'SimplePoco.SerializeAsArray',
        'SimplePoco.DeserializeAsArray'
    )
    
    Write-Host "📈 Reading current benchmark results..."
    $currentBenchmarkData = Get-Content $BenchmarkResultsPath -Raw | ConvertFrom-Json
    
    foreach ($benchmarkName in $trackedBenchmarks) {
        Write-Host "📊 Analyzing benchmark: $benchmarkName"
        
        # Get historical data for this benchmark
        $benchmarkQuery = "PartitionKey eq 'Benchmark' and MethodName eq '$benchmarkName'"
        $benchmarkEntities = Get-AzTableRow -Table $table.CloudTable -CustomFilter $benchmarkQuery |
                            Sort-Object BuildNumber -Descending |
                            Select-Object -First 10 |
                            Sort-Object BuildNumber
        
        if ($benchmarkEntities.Count -eq 0) {
            Write-Warning "  ⚠️ No historical data found for $benchmarkName"
            continue
        }
        
        # Find current benchmark result
        $currentBenchmark = $currentBenchmarkData.Benchmarks | Where-Object { $_.MethodTitle -eq $benchmarkName }
        if (-not $currentBenchmark) {
            Write-Warning "  ⚠️ Current benchmark result not found for $benchmarkName"
            continue
        }
        
        # Calculate regression
        $historicalMeans = $benchmarkEntities | ForEach-Object { [double]$_.Mean }
        $meanOfMeans = ($historicalMeans | Measure-Object -Average).Average
        $stdDevOfMeans = [Math]::Sqrt(($historicalMeans | ForEach-Object { [Math]::Pow($_ - $meanOfMeans, 2) } | Measure-Object -Average).Average)
        
        $currentMean = $currentBenchmark.Statistics.Mean
        $regressionThreshold = $meanOfMeans + 2 * $stdDevOfMeans
        $isRegression = $currentMean -gt $regressionThreshold
        
        Write-Host "    📈 Historical mean: $([math]::Round($meanOfMeans, 2)) ns, StdDev: $([math]::Round($stdDevOfMeans, 2)) ns"
        Write-Host "    📈 Current mean: $([math]::Round($currentMean, 2)) ns"
        Write-Host "    📈 Regression threshold: $([math]::Round($regressionThreshold, 2)) ns"
        
        if ($isRegression) {
            Write-Warning "    ⚠️ Performance regression detected!"
            $benchmarkRegressions += $benchmarkName
        } else {
            Write-Host "    ✅ Performance within acceptable range"
        }
    }
    
    # Generate summary report
    $summaryReport = @"
# Performance Comparison Report

## AOT Native Console File Size
- **Current Size:** $AotFileSizeMB MB
- **Regression Detected:** $($aotRegression ? 'YES ❌' : 'NO ✅')

## Benchmark Performance
"@

    foreach ($benchmarkName in $trackedBenchmarks) {
        $isRegression = $benchmarkName -in $benchmarkRegressions
        $summaryReport += @"

### $benchmarkName
- **Regression Detected:** $($isRegression ? 'YES ❌' : 'NO ✅')
"@
    }
    
    $summaryReport += @"

## Overall Result
- **Total Regressions:** $($benchmarkRegressions.Count + ($aotRegression ? 1 : 0))
- **Build Status:** $(($benchmarkRegressions.Count -eq 0 -and -not $aotRegression) ? 'PASS ✅' : 'FAIL ❌')

---
*Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')*
"@
    
    # Write summary report
    $summaryPath = Join-Path $OutputPath "performance-summary.md"
    $summaryReport | Out-File -FilePath $summaryPath -Encoding UTF8
    Write-Host "📄 Summary report written to: $summaryPath"
    
    # Set GitHub environment variables for workflow
    $hasRegressions = ($benchmarkRegressions.Count -gt 0) -or $aotRegression
    Write-Output "PERFORMANCE_REGRESSIONS=$hasRegressions" >> $env:GITHUB_ENV
    Write-Output "BENCHMARK_REGRESSIONS=$($benchmarkRegressions -join ',')" >> $env:GITHUB_ENV
    Write-Output "AOT_REGRESSION=$aotRegression" >> $env:GITHUB_ENV
    Write-Output "SUMMARY_REPORT_PATH=$summaryPath" >> $env:GITHUB_ENV
    
    if ($hasRegressions) {
        Write-Warning "❌ Performance regressions detected - build should fail"
        exit 1
    } else {
        Write-Host "✅ All performance metrics within acceptable range"
        exit 0
    }
}
catch {
    Write-Error "❌ Failed to analyze performance data: $($_.Exception.Message)"
    throw
}