# Batch Monitor Setup Script

Write-Host "Setting up Batch Monitor Dashboard..." -ForegroundColor Green

# Create sample directories and files for testing
$sampleDir = "C:\BatchMonitor_Sample"
$logsDir = "$sampleDir\Logs"
$configDir = "$sampleDir\Config"

if (-not (Test-Path $sampleDir)) {
    New-Item -ItemType Directory -Path $sampleDir -Force
    Write-Host "Created sample directory: $sampleDir" -ForegroundColor Yellow
}

if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir -Force
    Write-Host "Created logs directory: $logsDir" -ForegroundColor Yellow
}

if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir -Force
    Write-Host "Created config directory: $configDir" -ForegroundColor Yellow
}

# Create sample log files
$sampleLogs = @{
    "daily_process.log" = @"
2025-07-16 10:30:00 - Starting daily data processing
2025-07-16 10:30:05 - Connecting to database
2025-07-16 10:30:10 - Processing records...
2025-07-16 10:35:00 - Processed 1000 records
2025-07-16 10:35:05 - Data processing completed successfully
"@
    
    "weekly_report.log" = @"
2025-07-15 08:00:00 - Starting weekly report generation
2025-07-15 08:00:10 - Gathering data from sources
2025-07-15 08:05:00 - Warning: Data source 'Sales DB' is unavailable
2025-07-15 08:05:05 - Using cached data for Sales DB
2025-07-15 08:10:00 - Report generation completed with warnings
"@
    
    "cleanup.log" = @"
2025-07-16 02:00:00 - Starting file cleanup process
2025-07-16 02:00:05 - Scanning directory: C:\Temp
2025-07-16 02:00:10 - Error: Access denied to directory C:\Temp\System
2025-07-16 02:00:15 - Cleanup process failed
"@
}

foreach ($logFile in $sampleLogs.Keys) {
    $logPath = Join-Path $logsDir $logFile
    $sampleLogs[$logFile] | Out-File -FilePath $logPath -Encoding UTF8
    Write-Host "Created sample log: $logPath" -ForegroundColor Cyan
}

# Create sample PowerShell batch files
$sampleBatches = @{
    "daily_process.ps1" = @"
# Daily Data Processing Script
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Starting daily data processing" | Tee-Object -FilePath '$logsDir\daily_process.log' -Append
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Connecting to database" | Tee-Object -FilePath '$logsDir\daily_process.log' -Append
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Processing records..." | Tee-Object -FilePath '$logsDir\daily_process.log' -Append

# Simulate processing
Start-Sleep -Seconds 2

Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Processed 1000 records" | Tee-Object -FilePath '$logsDir\daily_process.log' -Append
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Data processing completed successfully" | Tee-Object -FilePath '$logsDir\daily_process.log' -Append
"@
    
    "weekly_report.ps1" = @"
# Weekly Report Generation Script
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Starting weekly report generation" | Tee-Object -FilePath '$logsDir\weekly_report.log' -Append
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Gathering data from sources" | Tee-Object -FilePath '$logsDir\weekly_report.log' -Append

# Simulate warning condition
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Warning: Data source 'Sales DB' is unavailable" | Tee-Object -FilePath '$logsDir\weekly_report.log' -Append
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Using cached data for Sales DB" | Tee-Object -FilePath '$logsDir\weekly_report.log' -Append
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Report generation completed with warnings" | Tee-Object -FilePath '$logsDir\weekly_report.log' -Append
"@
    
    "cleanup.ps1" = @"
# File Cleanup Script
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Starting file cleanup process" | Tee-Object -FilePath '$logsDir\cleanup.log' -Append
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Scanning directory: C:\Temp" | Tee-Object -FilePath '$logsDir\cleanup.log' -Append

# Simulate error condition
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Error: Access denied to directory C:\Temp\System" | Tee-Object -FilePath '$logsDir\cleanup.log' -Append
Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Cleanup process failed" | Tee-Object -FilePath '$logsDir\cleanup.log' -Append
"@
}

foreach ($batchFile in $sampleBatches.Keys) {
    $batchPath = Join-Path $logsDir $batchFile
    $sampleBatches[$batchFile] | Out-File -FilePath $batchPath -Encoding UTF8
    Write-Host "Created sample PowerShell batch: $batchPath" -ForegroundColor Green
}

# Create sample config files
$sampleConfigs = @{
    "daily_process.config" = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <appSettings>
        <add key="DatabaseConnection" value="Server=localhost;Database=DataDB;Integrated Security=true" />
        <add key="ProcessingBatchSize" value="1000" />
        <add key="TimeoutMinutes" value="30" />
    </appSettings>
</configuration>
"@
    
    "weekly_report.config" = @"
{
    "ReportSettings": {
        "OutputPath": "C:\\Reports\\Weekly",
        "Format": "PDF",
        "EmailRecipients": ["admin@company.com", "manager@company.com"]
    },
    "DataSources": {
        "SalesDB": "Server=sales-db;Database=Sales;Integrated Security=true",
        "InventoryDB": "Server=inv-db;Database=Inventory;Integrated Security=true"
    }
}
"@
    
    "cleanup.config" = @"
[Settings]
CleanupPath=C:\Temp
RetentionDays=30
ExcludePatterns=*.sys,*.dll
LogLevel=INFO
"@
}

foreach ($configFile in $sampleConfigs.Keys) {
    $configPath = Join-Path $configDir $configFile
    $sampleConfigs[$configFile] | Out-File -FilePath $configPath -Encoding UTF8
    Write-Host "Created sample config: $configPath" -ForegroundColor Cyan
}

# Update the sample batches.json with correct paths
$currentDate = Get-Date
$tomorrowDate = $currentDate.AddDays(1)
$yesterdayDate = $currentDate.AddDays(-1)
$nextWeekDate = $currentDate.AddDays(6)

$sampleBatchesJson = @"
[
  {
    "Name": "Daily Data Processing",
    "LogFilePath": "$($logsDir.Replace('\','\\'))\\daily_process.log",
    "ConfigFilePath": "$($configDir.Replace('\','\\'))\\daily_process.config",
    "Status": "Success",
    "LastRun": "$($currentDate.ToString('yyyy-MM-ddTHH:mm:ss'))",
    "NextRun": "$($tomorrowDate.ToString('yyyy-MM-ddTHH:mm:ss'))",
    "StatusMessage": "Processing completed successfully - 1000 records processed",
    "IsScheduled": true
  },
  {
    "Name": "Weekly Report Generation",
    "LogFilePath": "$($logsDir.Replace('\','\\'))\\weekly_report.log",
    "ConfigFilePath": "$($configDir.Replace('\','\\'))\\weekly_report.config",
    "Status": "Warning",
    "LastRun": "$($yesterdayDate.ToString('yyyy-MM-ddTHH:mm:ss'))",
    "NextRun": "$($nextWeekDate.ToString('yyyy-MM-ddTHH:mm:ss'))",
    "StatusMessage": "Warning: Some data sources were unavailable",
    "IsScheduled": true
  },
  {
    "Name": "File Cleanup",
    "LogFilePath": "$($logsDir.Replace('\','\\'))\\cleanup.log",
    "ConfigFilePath": "$($configDir.Replace('\','\\'))\\cleanup.config",
    "Status": "Error",
    "LastRun": "$($currentDate.ToString('yyyy-MM-ddTHH:mm:ss'))",
    "NextRun": "$($tomorrowDate.ToString('yyyy-MM-ddTHH:mm:ss'))",
    "StatusMessage": "Error: Access denied to cleanup directory",
    "IsScheduled": false
  }
]
"@

$batchesPath = Join-Path $PSScriptRoot "BatchMonitor\batches.json"
$sampleBatchesJson | Out-File -FilePath $batchesPath -Encoding UTF8
Write-Host "Updated batches.json with sample data" -ForegroundColor Green

Write-Host "`nSetup completed successfully!" -ForegroundColor Green
Write-Host "Sample files created in: $sampleDir" -ForegroundColor Yellow
Write-Host "`nTo build and run the application:" -ForegroundColor White
Write-Host "1. dotnet restore" -ForegroundColor Cyan
Write-Host "2. dotnet build" -ForegroundColor Cyan
Write-Host "3. dotnet run --project BatchMonitor" -ForegroundColor Cyan
