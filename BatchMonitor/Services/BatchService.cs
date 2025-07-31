using BatchMonitor.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BatchMonitor.Services
{
    public class BatchService
    {
        // SMTP configuration (simple, hardcoded for demo)
        private readonly string _smtpHost = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser = "arpitagrawal1207@gmail.com"; // sender (must allow less secure apps or use app password)
        private readonly string _smtpPass = "njvm exlb gypo fyyp"; // TODO: Replace with your app password
        private readonly string _mailTo = "arpit.agrawal2021@glbajajgroup.org";

        // Call this after analyzing all batches to send a summary email
        public async Task SendBatchReportEmailAsync(List<BatchItem> batches, DateTime filterDate)
        {
            try
            {
                var subject = $"BatchMonitor Report - {filterDate:yyyy-MM-dd}";
                var body = GenerateBatchReport(batches, filterDate);

                // First send email
                try
                {
                    using (var client = new System.Net.Mail.SmtpClient(_smtpHost, _smtpPort))
                    {
                        client.EnableSsl = true;
                        client.Credentials = new System.Net.NetworkCredential(_smtpUser, _smtpPass);

                        var mail = new System.Net.Mail.MailMessage(_smtpUser, _mailTo, subject, body);
                        mail.IsBodyHtml = true;
                        await client.SendMailAsync(mail);
                        System.Diagnostics.Debug.WriteLine("Email sent successfully");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to send email: {ex.Message}");
                }

                // Then send to Telegram
                try
                {
                    string botToken = "8492894039:AAGHMFW9Upj349sTr9AW4n68at3ODmXYW88";
                    string chatId = "2038406616";

                    // Get failed batches
                    var failedBatches = batches.Where(b => b.Status.ToString().ToUpper() == "ERROR" || b.Status.ToString().ToUpper() == "FAILED").ToList();
                    
                    var messageBuilder = new System.Text.StringBuilder();
                    if (failedBatches.Any())
                    {
                        messageBuilder.AppendLine($"🚨 Failed Batches Report - {filterDate:yyyy-MM-dd}");
                        messageBuilder.AppendLine($"Total Failed: {failedBatches.Count}\n");

                        foreach (var batch in failedBatches)
                        {
                            messageBuilder.AppendLine($"📦 Batch: {batch.Name}");
                            messageBuilder.AppendLine($"⚠️ Status: {batch.Status}");
                            if (!string.IsNullOrEmpty(batch.StatusMessage))
                                messageBuilder.AppendLine($"💬 Message: {batch.StatusMessage}");
                            
                            if (batch.Issues?.Any() == true)
                            {
                                messageBuilder.AppendLine($"🔍 Issues ({batch.Issues.Count}):");
                                foreach (var issue in batch.Issues)
                                {
                                    messageBuilder.AppendLine($"- {issue.Type}: {issue.Message}");
                                    messageBuilder.AppendLine($"  File: {issue.FileName}, Line: {issue.LineNumber}");
                                }
                            }
                            messageBuilder.AppendLine(); // Empty line between batches
                        }

                        using var httpClient = new System.Net.Http.HttpClient();
                        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
                        
                        var payload = new
                        {
                            chat_id = chatId,
                            text = messageBuilder.ToString(),
                            parse_mode = "HTML"
                        };
                        
                        var json = JsonConvert.SerializeObject(payload);
                        var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        
                        var response = await httpClient.PostAsync(url, content);
                        var responseText = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Telegram API response: {response.StatusCode} {responseText}");


                        //now for whatsapp
                        var processInfo = new ProcessStartInfo("node", $"index.js \"{messageBuilder.ToString()}\"")
                        {
                            WorkingDirectory = @"D:\Users\USER1\Desktop\whats_Batchmonitor",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using (var process = Process.Start(processInfo))
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();
                            Console.WriteLine(output);
                        }
                    }
                    else
                    {
                        var successBatches = batches.Count(b => b.Status.ToString().ToUpper() == "SUCCESS");
                        if (successBatches == batches.Count)
                        {
                            messageBuilder.AppendLine($"✅ Batch Status Report - {filterDate:yyyy-MM-dd}");
                            messageBuilder.AppendLine("All batches running successfully!");
                            messageBuilder.AppendLine($"Total Batches: {batches.Count}");
                        }
                    }

                    // Send message if we have content
                    if (messageBuilder.Length > 0)
                    {
                        using var httpClient = new System.Net.Http.HttpClient();
                        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
                        
                        var payload = new
                        {
                            chat_id = chatId,
                            text = messageBuilder.ToString(),
                            parse_mode = "HTML"
                        };
                        
                        var json = JsonConvert.SerializeObject(payload);
                        var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        
                        var response = await httpClient.PostAsync(url, content);
                        var responseText = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Telegram API response: {response.StatusCode} {responseText}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No status update to report on Telegram");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to send Telegram message: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overall report sending failed: {ex.Message}");
            }
        }

        // Generates a simple text report for all batches
        private string GenerateBatchReport(List<BatchItem> batches, DateTime filterDate)
        {
            var totalBatches = batches.Count;
            var successBatches = batches.Count(b => b.Status.ToString().ToUpper() == "SUCCESS");
            var failedBatches = batches.Count(b => b.Status.ToString().ToUpper() == "ERROR");
            var runningBatches = batches.Count(b => b.Status.ToString().ToUpper() == "RUNNING");

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            margin: 0; 
            padding: 20px; 
            background-color: #f5f5f5; 
        }}
        .container {{ 
            max-width: 800px; 
            margin: 0 auto; 
            background-color: white; 
            border: 1px solid #ddd; 
        }}
        .header {{ 
            background-color: #4a5568; 
            color: white; 
            padding: 20px; 
            text-align: center; 
        }}
        .header h1 {{ 
            margin: 0; 
            font-size: 24px; 
        }}
        .header .date {{ 
            margin: 5px 0 0 0; 
            font-size: 14px; 
            opacity: 0.9; 
        }}
        .summary {{ 
            padding: 20px; 
            border-bottom: 1px solid #eee; 
        }}
        .summary-grid {{ 
            display: table; 
            width: 100%; 
            border-collapse: separate; 
            border-spacing: 1px; 
        }}
        .summary-item {{ 
            display: table-cell; 
            text-align: center; 
            padding: 15px; 
            background-color: #f8f9fa; 
            border: 1px solid #dee2e6; 
        }}
        .summary-number {{ 
            font-size: 32px; 
            font-weight: bold; 
            margin-bottom: 5px; 
        }}
        .summary-label {{ 
            font-size: 12px; 
            color: #666; 
            text-transform: uppercase; 
        }}
        .batch-section {{ 
            margin: 20px; 
        }}
        .batch-item {{ 
            border: 1px solid #ddd; 
            margin-bottom: 15px; 
        }}
        .batch-header {{ 
            padding: 12px 15px; 
            font-weight: bold; 
            color: white; 
        }}
        .status-success {{ background-color: #28a745; }}
        .status-failed {{ background-color: #dc3545; }}
        .status-running {{ background-color: #ffc107; color: #000; }}
        .status-completed {{ background-color: #17a2b8; }}
        .batch-details {{ 
            padding: 15px; 
            background-color: #fafafa; 
        }}
        .detail-row {{ 
            margin-bottom: 8px; 
        }}
        .detail-label {{ 
            font-weight: bold; 
            color: #333; 
            display: inline-block; 
            width: 100px; 
        }}
        .issues {{ 
            margin-top: 10px; 
        }}
        .issue-item {{ 
            background-color: #fff3cd; 
            border: 1px solid #ffeaa7; 
            padding: 8px 12px; 
            margin: 5px 0; 
            border-radius: 3px; 
        }}
        .footer {{ 
            padding: 15px; 
            text-align: center; 
            background-color: #f8f9fa; 
            color: #666; 
            font-size: 12px; 
            border-top: 1px solid #eee; 
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Batch Monitor Report</h1>
            <div class='date'>{filterDate:yyyy-MM-dd}</div>
        </div>
        
        <div class='summary'>
            <div class='summary-grid'>
                <div class='summary-item'>
                    <div class='summary-number'>{totalBatches}</div>
                    <div class='summary-label'>Total</div>
                </div>
                <div class='summary-item'>
                    <div class='summary-number'>{successBatches}</div>
                    <div class='summary-label'>Success</div>
                </div>
                <div class='summary-item'>
                    <div class='summary-number'>{failedBatches}</div>
                    <div class='summary-label'>Failed</div>
                </div>
                <div class='summary-item'>
                    <div class='summary-number'>{runningBatches}</div>
                    <div class='summary-label'>Running</div>
                </div>
            </div>
        </div>
        
        <div class='batch-section'>";

            foreach (var batch in batches)
            {
                var statusClass = batch.Status.ToString().ToUpper() switch
                {
                    "SUCCESS" => "status-success",
                    "ERROR" => "status-failed",    // Changed from "FAILED" to "ERROR"
                    "FAILED" => "status-failed",   // Keep both for compatibility
                    "RUNNING" => "status-running",
                    "COMPLETED" => "status-completed",
                    _ => "status-unknown"          // Grey for unknown statuses
                };

                var statusLabel = batch.Status.ToString().ToUpper();

                html += $@"
            <div class='batch-item'>
                <div class='batch-header {statusClass}'>
                    {batch.Name} - {statusLabel}
                </div>
                <div class='batch-details'>
                    <div class='detail-row'>
                        <span class='detail-label'>Last Run:</span>
                        {batch.LastRun:yyyy-MM-dd HH:mm}
                    </div>
                    <div class='detail-row'>
                        <span class='detail-label'>Next Run:</span>
                        {batch.NextRun:yyyy-MM-dd HH:mm}
                    </div>
                    <div class='detail-row'>
                        <span class='detail-label'>Issues:</span>
                        {batch.Issues?.Count ?? 0}
                    </div>";

                if (batch.Issues != null && batch.Issues.Count > 0)
                {
                    html += "<div class='issues'>";
                    foreach (var issue in batch.Issues)
                    {
                        html += $@"<div class='issue-item'>
                            <b>Type:</b> {issue.Type}<br/>
                            <b>Message:</b> {issue.Message}<br/>
                            <b>Line:</b> {issue.LineNumber}<br/>
                            <b>File:</b> {issue.FileName}<br/>
                            <b>Time:</b> {issue.Timestamp}
                        </div>";
                    }
                    html += "</div>";
                }

                if (!string.IsNullOrEmpty(batch.StatusMessage))
                {
                    html += $@"
                    <div class='detail-row'>
                        <span class='detail-label'>Message:</span>
                        {batch.StatusMessage}
                    </div>";
                }

                html += @"
                </div>
            </div>";
            }

            html += $@"
        </div>
        
        <div class='footer'>
            Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Batch Monitoring System
        </div>
    </div>
</body>
</html>";

            return html;
        }

        private readonly CustomLogAnalyzer _customLogAnalyzer;
        private readonly PowerShellSchedulerService _taskScheduler;
        private readonly string _configPath;

        public BatchService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "BatchMonitor");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
            _configPath = Path.Combine(appFolder, "batches.json");
            _customLogAnalyzer = new CustomLogAnalyzer();
            _taskScheduler = new PowerShellSchedulerService();
            MigrateExistingData();
        }

        public class BatchAnalysisResult
        {
            public BatchStatus Status { get; set; }
            public string StatusMessage { get; set; } = string.Empty;
            public DateTime LastRun { get; set; }
            public List<string> DiscoveredLogFiles { get; set; } = new List<string>();
            public List<LogIssue> Issues { get; set; } = new List<LogIssue>();
            public bool IsScheduled { get; set; }
            public DateTime NextRun { get; set; }
        }

        public BatchAnalysisResult AnalyzeBatchForViewModel(BatchItem batch, DateTime filterDate)
        {
            var analysisResult = _customLogAnalyzer.AnalyzeBatch(batch, filterDate);
            var result = new BatchAnalysisResult
            {
                Status = analysisResult.Status,
                StatusMessage = analysisResult.StatusMessage,
                LastRun = analysisResult.LastRunTime,
                DiscoveredLogFiles = analysisResult.DiscoveredLogFiles,
                Issues = analysisResult.Issues,
                IsScheduled = _taskScheduler.IsScheduled(batch.Name),
                NextRun = _taskScheduler.GetNextRunTime(batch.Name) ?? DateTime.MinValue
            };
            if (batch.BatchType == BatchType.Hourly && analysisResult.DiscoveredLogFiles.Any())
            {
                var primaryLogFile = analysisResult.DiscoveredLogFiles.First();
                var zipPath = _customLogAnalyzer.GetDailyZipPath(primaryLogFile);
                if (File.Exists(zipPath))
                    result.StatusMessage += $" (Previous day archived: {Path.GetFileName(zipPath)})";
                if (!_customLogAnalyzer.IsLogFileRecentlyUpdated(primaryLogFile, 2))
                    result.StatusMessage += " (Log not recently updated)";
            }
            return result;
        }

        private void MigrateExistingData()
        {
            try
            {
                // Check if there's existing data in the old location (bin folder)
                var oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "batches.json");

                if (File.Exists(oldPath) && !File.Exists(_configPath))
                {
                    // Copy data from old location to new AppData location
                    var oldData = File.ReadAllText(oldPath);
                    File.WriteAllText(_configPath, oldData);
                }
                else if (File.Exists(_configPath))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Config file already exists at: {_configPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ℹ️ No existing data to migrate");
                }
            }
            catch (Exception)
            {
                // Ignore migration errors
            }
        }

        public async Task<List<BatchItem>> LoadBatchesAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return new List<BatchItem>();
                var json = await Task.Run(() => File.ReadAllText(_configPath));
                var batches = JsonConvert.DeserializeObject<List<BatchItem>>(json) ?? new List<BatchItem>();
                // Do NOT update status, issues, discovered log files, etc. Only load config info.
                return batches;
            }
            catch
            {
                return new List<BatchItem>();
            }
        }

        public void SaveBatches(List<BatchItem> batches)
        {
            try
            {
                // Only store minimal config info for each batch
                var minimalBatches = batches.Select(b => new BatchItem
                {
                    Name = b.Name,
                    LogFilePath = b.LogFilePath,
                    ErrorLogFilePath = b.ErrorLogFilePath,
                    CustomLogFilePath = b.CustomLogFilePath,
                    ConfigFilePath = b.ConfigFilePath,
                    ExecutablePath = b.ExecutablePath,
                    BatchType = b.BatchType
                }).ToList();
                var json = JsonConvert.SerializeObject(minimalBatches, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save batches: {ex.Message}");
            }
        }

        // ...

        private void ApplyAnalysisResultToBatch(BatchItem batch, BatchAnalysisResult analysisResult)
        {
            batch.Status = analysisResult.Status;
            batch.StatusMessage = analysisResult.StatusMessage;
            batch.LastRun = analysisResult.LastRun;
            batch.DiscoveredLogFiles = new ObservableCollection<string>(analysisResult.DiscoveredLogFiles);
            batch.Issues = new ObservableCollection<LogIssue>(analysisResult.Issues);
            batch.IsScheduled = analysisResult.IsScheduled;
            batch.NextRun = analysisResult.NextRun;

            if (batch.BatchType == BatchType.Hourly && analysisResult.DiscoveredLogFiles.Any())
            {
                var primaryLogFile = analysisResult.DiscoveredLogFiles.First();
                var zipPath = _customLogAnalyzer.GetDailyZipPath(primaryLogFile);
                if (File.Exists(zipPath))
                    batch.StatusMessage += $" (Previous day archived: {Path.GetFileName(zipPath)})";
                if (!_customLogAnalyzer.IsLogFileRecentlyUpdated(primaryLogFile, 2))
                    batch.StatusMessage += " (Log not recently updated)";
            }
        }

        public async Task UpdateBatchStatusAsync(BatchItem batch)
        {
            try
            {
                var analysisResult = await _customLogAnalyzer.AnalyzeBatchAsync(batch);
                var result = new BatchAnalysisResult
                {
                    Status = analysisResult.Status,
                    StatusMessage = analysisResult.StatusMessage,
                    LastRun = analysisResult.LastRunTime,
                    DiscoveredLogFiles = analysisResult.DiscoveredLogFiles,
                    Issues = analysisResult.Issues,
                    IsScheduled = _taskScheduler.IsScheduled(batch.Name),
                    NextRun = _taskScheduler.GetNextRunTime(batch.Name) ?? DateTime.MinValue
                };
                ApplyAnalysisResultToBatch(batch, result);
            }
            catch (Exception ex)
            {
                batch.Status = BatchStatus.Error;
                batch.StatusMessage = $"Error analyzing logs: {ex.Message}";
            }
        }

        public void UpdateBatchStatus(BatchItem batch, DateTime filterDate)
        {
            try
            {
                var analysisResult = _customLogAnalyzer.AnalyzeBatch(batch, filterDate);
                var result = new BatchAnalysisResult
                {
                    Status = analysisResult.Status,
                    StatusMessage = analysisResult.StatusMessage,
                    LastRun = analysisResult.LastRunTime,
                    DiscoveredLogFiles = analysisResult.DiscoveredLogFiles,
                    Issues = analysisResult.Issues,
                    IsScheduled = _taskScheduler.IsScheduled(batch.Name),
                    NextRun = _taskScheduler.GetNextRunTime(batch.Name) ?? DateTime.MinValue
                };
                ApplyAnalysisResultToBatch(batch, result);
            }
            catch (Exception ex)
            {
                batch.Status = BatchStatus.Error;
                batch.StatusMessage = $"Error analyzing logs: {ex.Message}";
            }
        }

        public async Task UpdateBatchStatusAsync(BatchItem batch, DateTime filterDate)
        {
            try
            {
                var analysisResult = await Task.Run(() => _customLogAnalyzer.AnalyzeBatch(batch, filterDate));
                var result = new BatchAnalysisResult
                {
                    Status = analysisResult.Status,
                    StatusMessage = analysisResult.StatusMessage,
                    LastRun = analysisResult.LastRunTime,
                    DiscoveredLogFiles = analysisResult.DiscoveredLogFiles,
                    Issues = analysisResult.Issues,
                    IsScheduled = _taskScheduler.IsScheduled(batch.Name),
                    NextRun = _taskScheduler.GetNextRunTime(batch.Name) ?? DateTime.MinValue
                };
                ApplyAnalysisResultToBatch(batch, result);
            }
            catch (Exception ex)
            {
                batch.Status = BatchStatus.Error;
                batch.StatusMessage = $"Error analyzing logs: {ex.Message}";
            }
        }

        public void ScheduleBatch(BatchItem batch, DateTime startTime)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            try
            {
                // Use specified executable path if provided, otherwise try to find it
                var batchPath = !string.IsNullOrEmpty(batch.ExecutablePath) ?
                    batch.ExecutablePath :
                    FindBatchExecutable(batch.LogFilePath);

                if (string.IsNullOrEmpty(batchPath))
                {
                    throw new Exception($"Cannot find executable file for batch '{batch.Name}'. Please specify the executable path (e.g., StartServices.exe) when adding the batch.");
                }

                // Verify the executable exists
                if (!File.Exists(batchPath))
                {
                    throw new Exception($"Executable file not found: {batchPath}");
                }

                // Schedule the task
                _taskScheduler.ScheduleBatch(batch.Name, batchPath, startTime, batch.BatchType);

                // Verify the task was actually created
                System.Threading.Thread.Sleep(1000); // Give Windows a moment to register the task

                var isScheduled = _taskScheduler.IsScheduled(batch.Name);
                var nextRun = _taskScheduler.GetNextRunTime(batch.Name);

                if (!isScheduled)
                {
                    throw new Exception("Task was not successfully registered in Windows Task Scheduler");
                }

                // Update batch properties with actual values from task scheduler
                batch.IsScheduled = isScheduled;
                batch.NextRun = nextRun ?? startTime;
            }
            catch (Exception ex)
            {
                // Reset scheduling status on error
                batch.IsScheduled = false;

                throw new Exception($"Failed to schedule batch '{batch.Name}': {ex.Message}");
            }
        }

        private string FindBatchExecutable(string logFilePath)
        {
            if (string.IsNullOrEmpty(logFilePath))
                return string.Empty;

            var directory = Path.GetDirectoryName(logFilePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(logFilePath);

            if (string.IsNullOrEmpty(directory))
                return string.Empty;

            // List of executable extensions to look for
            var executableExtensions = new[] { ".exe", ".ps1", ".bat", ".cmd" };

            // Look in same directory as log file
            foreach (var ext in executableExtensions)
            {
                var executablePath = Path.Combine(directory, fileNameWithoutExtension + ext);
                if (File.Exists(executablePath))
                    return executablePath;
            }

            // Look for StartServices.exe specifically
            var startServicesPath = Path.Combine(directory, "StartServices.exe");
            if (File.Exists(startServicesPath))
                return startServicesPath;

            // Look in parent directory
            var parentDir = Path.GetDirectoryName(directory);
            if (!string.IsNullOrEmpty(parentDir))
            {
                foreach (var ext in executableExtensions)
                {
                    var executablePath = Path.Combine(parentDir, fileNameWithoutExtension + ext);
                    if (File.Exists(executablePath))
                        return executablePath;
                }

                // Check for StartServices.exe in parent directory
                startServicesPath = Path.Combine(parentDir, "StartServices.exe");
                if (File.Exists(startServicesPath))
                    return startServicesPath;
            }

            // Look for any .exe file with similar name pattern
            if (Directory.Exists(directory))
            {
                var exeFiles = Directory.GetFiles(directory, "*.exe");
                foreach (var exeFile in exeFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(exeFile);
                    // Check if filename contains the log name or common patterns
                    if (fileName.Contains(fileNameWithoutExtension) ||
                        fileNameWithoutExtension.Contains(fileName) ||
                        fileName.ToLower().Contains("start") ||
                        fileName.ToLower().Contains("service"))
                    {
                        return exeFile;
                    }
                }
            }

            return string.Empty;
        }

        public void DeleteSchedule(BatchItem batch)
        {
            try
            {
                _taskScheduler.DeleteSchedule(batch.Name);
                batch.IsScheduled = false;
                batch.NextRun = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete schedule: {ex.Message}");
            }
        }

        public string ReadLogFile(string logFilePath)
        {
            try
            {
                if (!File.Exists(logFilePath))
                    return "Log file not found.";

                return File.ReadAllText(logFilePath);
            }
            catch (Exception ex)
            {
                return $"Error reading log file: {ex.Message}";
            }
        }

        public string ReadConfigFile(string configFilePath)
        {
            try
            {
                if (!File.Exists(configFilePath))
                    return "Config file not found.";

                return File.ReadAllText(configFilePath);
            }
            catch (Exception ex)
            {
                return $"Error reading config file: {ex.Message}";
            }
        }

        public bool IsScheduled(string batchName)
        {
            return _taskScheduler.IsScheduled(batchName);
        }

        public DateTime? GetNextRunTime(string batchName)
        {
            return _taskScheduler.GetNextRunTime(batchName);
        }

        public void Dispose()
        {
            // PowerShellSchedulerService doesn't need disposal
        }


    }
}
