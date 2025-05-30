# Performance Tracking System

This directory contains scripts and tools for tracking performance benchmarks and AOT file sizes in the Nerdbank.MessagePack project.

## Overview

The performance tracking system automatically:
- Runs specific benchmarks on every build
- Measures AotNativeConsole file size
- Stores historical data in Azure Table Storage (CI builds only)
- Compares PR performance against historical trends
- Fails PRs with significant performance regressions
- Posts detailed results as PR comments

## Components

### GitHub Actions Workflow
- **File**: `.github/workflows/perf.yml`
- **Triggers**: Push to main branch, pull requests
- **Purpose**: Orchestrates the entire performance tracking process

### PowerShell Scripts

#### Store-PerformanceData.ps1
Stores benchmark results and AOT file sizes to Azure Table Storage for CI builds on the main branch.

**Parameters:**
- `StorageAccount`: Azure Storage Account name
- `TableName`: Azure Table name
- `BenchmarkResultsPath`: Path to BenchmarkDotNet JSON results
- `AotFileSizeMB`: AOT file size in MB
- Version information from nbgv

#### Compare-PerformanceData.ps1
Retrieves historical data and compares current PR performance against trends.

**Features:**
- Downloads last 10 CI builds from Azure Table Storage
- Calculates statistical thresholds (mean + 2 standard deviations)
- Detects performance regressions
- Generates comparison reports

#### Post-PerfComment.ps1
Posts detailed performance analysis results as PR comments.

## Tracked Metrics

### Benchmarks
The following benchmarks from `SimplePoco` class are tracked:
- `SimplePoco.DeserializeMapInit`
- `SimplePoco.DeserializeMap`
- `SimplePoco.SerializeMap`
- `SimplePoco.SerializeAsArray`
- `SimplePoco.DeserializeAsArray`

### AOT File Size
The compiled size of the `AotNativeConsole` project is monitored to detect binary size regressions.

## Regression Detection

Performance regressions are detected when:
- Benchmark execution time exceeds historical mean + 2 standard deviations
- AOT file size exceeds historical mean + 2 standard deviations

Any detected regression will:
1. Fail the PR build
2. Post a detailed explanation in PR comments
3. Set appropriate GitHub status checks

## Azure Setup Requirements

The system requires an Azure Storage Account with:
- Table Storage enabled
- Managed Identity authentication configured
- GitHub Actions runner with appropriate Azure permissions

### Required Environment Variables
- `AZURE_STORAGE_ACCOUNT`: Storage account name
- `AZURE_TABLE_NAME`: Table name for storing data

## Data Structure

### Azure Table Storage Schema

#### AOT File Size Entities
- **PartitionKey**: "AotFileSize"
- **RowKey**: CloudBuildNumber
- **Fields**: CommitId, Version info, FileSizeMB

#### Benchmark Entities
- **PartitionKey**: "Benchmark"
- **RowKey**: "{CloudBuildNumber}-{MethodName}"
- **Fields**: CommitId, Version info, MethodName, Mean, StdDev, Min, Max, Allocated

## Usage

### For CI Builds (Main Branch)
1. Workflow runs automatically on push to main
2. Benchmarks are executed
3. Results are stored in Azure Table Storage
4. Build succeeds/fails based on basic execution

### For Pull Requests
1. Workflow runs automatically on PR creation/updates
2. Benchmarks are executed
3. Historical data is retrieved from Azure
4. Statistical comparison is performed
5. Results are posted as PR comments
6. Build fails if significant regressions are detected

## Troubleshooting

### Common Issues
1. **Azure Authentication Failures**: Ensure managed identity is properly configured
2. **Missing Historical Data**: First few PRs may not have enough data for comparison
3. **Benchmark Execution Failures**: Check that all tracked benchmarks exist in SimplePoco class

### Debug Information
All scripts include verbose logging to help diagnose issues. Check GitHub Actions logs for detailed execution information.