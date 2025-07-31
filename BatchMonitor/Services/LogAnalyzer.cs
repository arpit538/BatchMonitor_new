using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BatchMonitor.Models;

namespace BatchMonitor.Services
{
    public class LogAnalyzer
    {
        private readonly List<string> _errorKeywords = new()
        {
            "error", "failed", "failure", "fatal", "crash", "abort"
        };

        private readonly List<string> _warningKeywords = new()
        {
            "warning", "warn", "deprecated", "caution", "alert"
        };

        private readonly List<string> _successKeywords = new()
        {
            "successfully", "completed", "finished", "done", "processed"
        };

        public BatchStatus AnalyzeLogFile(string logFilePath)
        {
            return AnalyzeLogFiles(logFilePath, string.Empty);
        }

        public BatchStatus AnalyzeLogFiles(string logFilePath, string? errorLogFilePath)
        {
            try
            {
                var mainLogStatus = BatchStatus.Unknown;
                var errorLogStatus = BatchStatus.Unknown;

                // Analyze main log file
                if (!string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
                {
                    var logContent = File.ReadAllText(logFilePath).ToLower();
                    mainLogStatus = AnalyzeContent(logContent);
                }

                // Analyze error log file
                if (!string.IsNullOrEmpty(errorLogFilePath) && File.Exists(errorLogFilePath))
                {
                    var errorContent = File.ReadAllText(errorLogFilePath).ToLower();
                    errorLogStatus = AnalyzeContent(errorContent);
                    
                    // Check if error log has recent entries
                    var errorFileInfo = new FileInfo(errorLogFilePath);
                    if (errorFileInfo.Length > 0 && errorFileInfo.LastWriteTime > DateTime.Now.AddHours(-24))
                    {
                        // Recent errors found, prioritize error status
                        if (errorLogStatus == BatchStatus.Error)
                            return BatchStatus.Error;
                    }
                }

                // Priority: Error > Warning > Success > Unknown
                if (mainLogStatus == BatchStatus.Error || errorLogStatus == BatchStatus.Error)
                    return BatchStatus.Error;
                
                if (mainLogStatus == BatchStatus.Warning || errorLogStatus == BatchStatus.Warning)
                    return BatchStatus.Warning;
                
                if (mainLogStatus == BatchStatus.Success || errorLogStatus == BatchStatus.Success)
                    return BatchStatus.Success;

                return BatchStatus.Unknown;
            }
            catch
            {
                return BatchStatus.Error;
            }
        }

        private BatchStatus AnalyzeContent(string content)
        {
            // Check for errors first (highest priority)
            if (ContainsKeywords(content, _errorKeywords))
                return BatchStatus.Error;

            // Check for warnings
            if (ContainsKeywords(content, _warningKeywords))
                return BatchStatus.Warning;

            // Check for success indicators
            if (ContainsKeywords(content, _successKeywords))
                return BatchStatus.Success;

            return BatchStatus.Unknown;
        }

        public string GetStatusMessage(string logFilePath)
        {
            return GetStatusMessage(logFilePath, string.Empty);
        }

        public string GetStatusMessage(string logFilePath, string? errorLogFilePath)
        {
            try
            {
                var mainLogMessage = string.Empty;
                var errorLogMessage = string.Empty;

                // Get message from main log
                if (!string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
                {
                    var lines = File.ReadAllLines(logFilePath);
                    var status = AnalyzeContent(File.ReadAllText(logFilePath).ToLower());
                    mainLogMessage = GetMessageByStatus(lines, status);
                }

                // Get message from error log
                if (!string.IsNullOrEmpty(errorLogFilePath) && File.Exists(errorLogFilePath))
                {
                    var errorLines = File.ReadAllLines(errorLogFilePath);
                    var errorFileInfo = new FileInfo(errorLogFilePath);
                    
                    // Check if error log has recent entries
                    if (errorFileInfo.Length > 0 && errorFileInfo.LastWriteTime > DateTime.Now.AddHours(-24))
                    {
                        errorLogMessage = GetLastErrorMessage(errorLines);
                    }
                }

                // Prioritize error log message if available
                if (!string.IsNullOrEmpty(errorLogMessage))
                {
                    return $"Error: {errorLogMessage}";
                }

                return !string.IsNullOrEmpty(mainLogMessage) ? mainLogMessage : "No status information available";
            }
            catch (Exception ex)
            {
                return $"Error reading log files: {ex.Message}";
            }
        }

        private string GetMessageByStatus(string[] lines, BatchStatus status)
        {
            return status switch
            {
                BatchStatus.Success => GetLastSuccessMessage(lines),
                BatchStatus.Warning => GetLastWarningMessage(lines),
                BatchStatus.Error => GetLastErrorMessage(lines),
                _ => "No status information available"
            };
        }

        public DateTime GetLastRunTime(string logFilePath)
        {
            return GetLastRunTime(logFilePath, BatchType.FixedTime);
        }

        public DateTime GetLastRunTime(string logFilePath, BatchType batchType)
        {
            try
            {
                if (!File.Exists(logFilePath))
                    return DateTime.MinValue;

                var fileInfo = new FileInfo(logFilePath);
                
                if (batchType == BatchType.FixedTime)
                {
                    // For fixed time batches, use file modification time
                    return fileInfo.LastWriteTime;
                }
                else // Hourly batches
                {
                    // For hourly batches, parse last timestamp from log content
                    return GetLastTimestampFromLog(logFilePath);
                }
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private DateTime GetLastTimestampFromLog(string logFilePath)
        {
            try
            {
                var lines = File.ReadAllLines(logFilePath);
                
                // Look for timestamp patterns in reverse order
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    var line = lines[i];
                    
                    // Try to parse common timestamp formats
                    var timestampPatterns = new[]
                    {
                        @"(\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})",  // 2025-07-16 14:30:00
                        @"(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2}:\d{2})",  // 07/16/2025 14:30:00
                        @"(\d{2}-\d{2}-\d{4}\s\d{2}:\d{2}:\d{2})"   // 16-07-2025 14:30:00
                    };

                    foreach (var pattern in timestampPatterns)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, pattern);
                        if (match.Success)
                        {
                            if (DateTime.TryParse(match.Groups[1].Value, out var timestamp))
                            {
                                return timestamp;
                            }
                        }
                    }
                }

                // If no timestamp found, use file modification time
                return new FileInfo(logFilePath).LastWriteTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public bool IsLogFileRecentlyUpdated(string logFilePath, int hoursThreshold = 2)
        {
            try
            {
                if (!File.Exists(logFilePath))
                    return false;

                var fileInfo = new FileInfo(logFilePath);
                return fileInfo.LastWriteTime > DateTime.Now.AddHours(-hoursThreshold);
            }
            catch
            {
                return false;
            }
        }

        public string GetDailyZipPath(string logFilePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(logFilePath);
                if (string.IsNullOrEmpty(directory))
                    return string.Empty;

                var yesterday = DateTime.Now.AddDays(-1);
                var zipFileName = $"logs_{yesterday:yyyyMMdd}.zip";
                
                return Path.Combine(directory, zipFileName);
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool ContainsKeywords(string content, List<string> keywords)
        {
            return keywords.Any(keyword => content.Contains(keyword));
        }

        private string GetLastSuccessMessage(string[] lines)
        {
            var successLine = lines.Reverse()
                .FirstOrDefault(line => _successKeywords.Any(keyword => 
                    line.ToLower().Contains(keyword)));

            return successLine?.Trim() ?? "Batch completed successfully";
        }

        private string GetLastWarningMessage(string[] lines)
        {
            var warningLine = lines.Reverse()
                .FirstOrDefault(line => _warningKeywords.Any(keyword => 
                    line.ToLower().Contains(keyword)));

            return warningLine?.Trim() ?? "Warning detected in batch execution";
        }

        private string GetLastErrorMessage(string[] lines)
        {
            var errorLine = lines.Reverse()
                .FirstOrDefault(line => _errorKeywords.Any(keyword => 
                    line.ToLower().Contains(keyword)));

            return errorLine?.Trim() ?? "Error occurred during batch execution";
        }
    }
}
