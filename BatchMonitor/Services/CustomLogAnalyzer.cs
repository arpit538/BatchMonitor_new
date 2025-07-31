using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BatchMonitor.Models;

namespace BatchMonitor.Services
{
    public class CustomLogAnalyzer
    {
        private readonly LogAnalyzer _standardLogAnalyzer;

        public CustomLogAnalyzer()
        {
            _standardLogAnalyzer = new LogAnalyzer();
        }

        // Async version to prevent UI freezing
        public async Task<LogAnalysisResult> AnalyzeBatchAsync(BatchItem batch)
        {
            return await Task.Run(() => AnalyzeBatch(batch));
        }

        // Main analysis method for BatchService integration
        public LogAnalysisResult AnalyzeBatch(BatchItem batch)
        {
            var result = new LogAnalysisResult
            {
                Status = BatchStatus.Unknown,
                StatusMessage = "No log files found",
                LastRunTime = DateTime.MinValue,
                DiscoveredLogFiles = new List<string>(),
                Issues = new List<LogIssue>()
            };

            try
            {
                // Priority 1: Custom log file path (User-specified)
                if (!string.IsNullOrEmpty(batch.CustomLogFilePath) && File.Exists(batch.CustomLogFilePath))
                {
                    result.DiscoveredLogFiles.Add(batch.CustomLogFilePath);
                    var analysis = AnalyzeSingleLogFileWithIssues(batch.CustomLogFilePath);
                    result.Status = analysis.Status;
                    result.StatusMessage = analysis.StatusMessage;
                    result.LastRunTime = analysis.LastRunTime;
                    result.Issues = analysis.Issues;

                    // Update batch with issues
                    batch.Issues = new ObservableCollection<LogIssue>(result.Issues);
                    return result;
                }

                // Priority 2: Standard log file paths (backward compatibility)
                if (!string.IsNullOrEmpty(batch.LogFilePath) && File.Exists(batch.LogFilePath))
                {
                    result.DiscoveredLogFiles.Add(batch.LogFilePath);

                    // Analyze main log file for issues
                    var mainAnalysis = AnalyzeSingleLogFileWithIssues(batch.LogFilePath);
                    result.Issues.AddRange(mainAnalysis.Issues);
                    result.Status = mainAnalysis.Status;
                    result.StatusMessage = mainAnalysis.StatusMessage;
                    result.LastRunTime = mainAnalysis.LastRunTime;

                    // Add error log to discovered files if exists
                    if (!string.IsNullOrEmpty(batch.ErrorLogFilePath) && File.Exists(batch.ErrorLogFilePath))
                    {
                        result.DiscoveredLogFiles.Add(batch.ErrorLogFilePath);
                        var errorAnalysis = AnalyzeSingleLogFileWithIssues(batch.ErrorLogFilePath);
                        result.Issues.AddRange(errorAnalysis.Issues);

                        // Error log takes precedence for status
                        if (errorAnalysis.Status == BatchStatus.Error)
                        {
                            result.Status = BatchStatus.Error;
                            result.StatusMessage = "Errors found in logs";
                        }
                    }

                    // Update batch with issues
                    // Replace the line causing the error with the following:
                    batch.Issues = new ObservableCollection<LogIssue>(result.Issues);
                    return result;
                }

                // Priority 3: Auto-discovery based on executable path
                if (!string.IsNullOrEmpty(batch.ExecutablePath))
                {
                    var directory = Path.GetDirectoryName(batch.ExecutablePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        var discoveredLogs = DiscoverLogFiles(directory);
                        if (discoveredLogs.Any())
                        {
                            result.DiscoveredLogFiles = discoveredLogs;
                            var analysis = AnalyzeSingleLogFile(discoveredLogs.First());
                            result.Status = analysis.Status;
                            result.StatusMessage = $"Auto-discovered: {analysis.StatusMessage}";
                            result.LastRunTime = analysis.LastRunTime;
                            return result;
                        }
                    }
                }

                result.StatusMessage = "No log files could be discovered for this batch";
                return result;
            }
            catch (Exception ex)
            {
                result.Status = BatchStatus.Error;
                result.StatusMessage = $"Error analyzing batch: {ex.Message}";
                return result;
            }
        }

        // Enhanced overloaded method with priority logic for date filtering
        public LogAnalysisResult AnalyzeBatch(BatchItem batch, DateTime filterDate)
        {
            var result = new LogAnalysisResult
            {
                Status = BatchStatus.Unknown,
                StatusMessage = "No log files found",
                LastRunTime = DateTime.MinValue,
                DiscoveredLogFiles = new List<string>(),
                Issues = new List<LogIssue>()
            };

            try
            {
                // Priority 1: Custom log file path (User-specified)
                if (!string.IsNullOrEmpty(batch.CustomLogFilePath) && File.Exists(batch.CustomLogFilePath))
                {
                    result.DiscoveredLogFiles.Add(batch.CustomLogFilePath);
                    var analysis = AnalyzeSingleLogFileWithPriorityLogic(batch.CustomLogFilePath, null, filterDate);
                    result.Status = analysis.Status;
                    result.StatusMessage = analysis.StatusMessage;
                    result.LastRunTime = analysis.LastRunTime;
                    result.Issues = analysis.Issues;
                    batch.Issues = new ObservableCollection<LogIssue>(result.Issues);
                    return result;
                }

                // Priority 2: Standard log file paths with dual file priority logic
                if (!string.IsNullOrEmpty(batch.LogFilePath) && File.Exists(batch.LogFilePath))
                {
                    result.DiscoveredLogFiles.Add(batch.LogFilePath);

                    string errorLogPath = null;
                    if (!string.IsNullOrEmpty(batch.ErrorLogFilePath) && File.Exists(batch.ErrorLogFilePath))
                    {
                        result.DiscoveredLogFiles.Add(batch.ErrorLogFilePath);
                        errorLogPath = batch.ErrorLogFilePath;
                    }

                    // Use priority logic for dual file analysis
                    var analysis = AnalyzeSingleLogFileWithPriorityLogic(batch.LogFilePath, errorLogPath, filterDate);
                    result.Status = analysis.Status;
                    result.StatusMessage = analysis.StatusMessage;
                    result.LastRunTime = analysis.LastRunTime;
                    result.Issues = analysis.Issues;
                    batch.Issues = new ObservableCollection<LogIssue>(result.Issues);
                    return result;
                }

                // Priority 3: Auto-discovery based on executable path
                if (!string.IsNullOrEmpty(batch.ExecutablePath))
                {
                    var directory = Path.GetDirectoryName(batch.ExecutablePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        var discoveredLogs = DiscoverLogFiles(directory);
                        if (discoveredLogs.Any())
                        {
                            result.DiscoveredLogFiles = discoveredLogs;
                            var analysis = AnalyzeSingleLogFileWithPriorityLogic(discoveredLogs.First(), null, filterDate);
                            result.Status = analysis.Status;
                            result.StatusMessage = $"Auto-discovered: {analysis.StatusMessage}";
                            result.LastRunTime = analysis.LastRunTime;
                            result.Issues = analysis.Issues;
                            return result;
                        }
                    }
                }

                result.StatusMessage = "No log files could be discovered for this batch";
                return result;
            }
            catch (Exception ex)
            {
                result.Status = BatchStatus.Error;
                result.StatusMessage = $"Error analyzing batch: {ex.Message}";
                return result;
            }
        }

        // New method implementing the priority logic you requested
        private LogAnalysisResult AnalyzeSingleLogFileWithPriorityLogic(string mainLogPath, string errorLogPath, DateTime filterDate)
        {
            var result = new LogAnalysisResult
            {
                Status = BatchStatus.Unknown,
                StatusMessage = "Unable to analyze log file",
                LastRunTime = DateTime.MinValue,
                DiscoveredLogFiles = new List<string>(),
                Issues = new List<LogIssue>()
            };

            try
            {
                // Add discovered files to result
                if (!string.IsNullOrEmpty(mainLogPath))
                    result.DiscoveredLogFiles.Add(mainLogPath);
                if (!string.IsNullOrEmpty(errorLogPath))
                    result.DiscoveredLogFiles.Add(errorLogPath);

                // Step 1: Check error log first (if exists)
                if (!string.IsNullOrEmpty(errorLogPath) && File.Exists(errorLogPath))
                {
                    var errorAnalysis = AnalyzeSingleLogFileWithIssues(errorLogPath, filterDate);
                    var errorIssuesForDate = errorAnalysis.Issues.Where(i => i.Timestamp.Date == filterDate.Date).ToList();

                    // Check if there are error entries for the filter date
                    var errorEntriesForDate = errorIssuesForDate.Where(i => i.Type == IssueType.Error).ToList();

                    if (errorEntriesForDate.Any())
                    {
                        result.Status = BatchStatus.Error;
                        result.StatusMessage = "Errors found in error log for the specified date";
                        result.Issues = errorEntriesForDate;
                        result.LastRunTime = errorAnalysis.LastRunTime;
                        return result;
                    }
                }

                // Step 2: Check main log file
                if (!string.IsNullOrEmpty(mainLogPath) && File.Exists(mainLogPath))
                {
                    var mainAnalysis = AnalyzeSingleLogFileWithIssues(mainLogPath, filterDate);
                    var mainIssuesForDate = mainAnalysis.Issues.Where(i => i.Timestamp.Date == filterDate.Date).ToList();

                    if (mainIssuesForDate.Any())
                    {
                        // Check for errors first
                        var errorEntriesForDate = mainIssuesForDate.Where(i => i.Type == IssueType.Error).ToList();
                        if (errorEntriesForDate.Any())
                        {
                            result.Status = BatchStatus.Error;
                            result.StatusMessage = "Errors found in main log for the specified date";
                            result.Issues = errorEntriesForDate;
                            result.LastRunTime = mainAnalysis.LastRunTime;
                            return result;
                        }

                        // Check for warnings
                        var warningEntriesForDate = mainIssuesForDate.Where(i => i.Type == IssueType.Warning).ToList();
                        if (warningEntriesForDate.Any())
                        {
                            result.Status = BatchStatus.Warning;
                            result.StatusMessage = "Warnings found in main log for the specified date";
                            result.Issues = warningEntriesForDate;
                            result.LastRunTime = mainAnalysis.LastRunTime;
                            return result;
                        }

                        // Check for info/success entries
                        var infoEntriesForDate = mainIssuesForDate.Where(i => i.Type == IssueType.Info).ToList();
                        if (infoEntriesForDate.Any())
                        {
                            result.Status = BatchStatus.Success;
                            result.StatusMessage = "Success entries found in main log for the specified date";
                            result.Issues = infoEntriesForDate;
                            result.LastRunTime = mainAnalysis.LastRunTime;
                            return result;
                        }
                    }
                }

                // Step 3: No entries found for the specified date
                result.Status = BatchStatus.Unknown;
                result.StatusMessage = $"No log entries found for the specified date ({filterDate.Date:yyyy-MM-dd})";
                result.Issues = new List<LogIssue>();

                // Set last run time from file modification if available
                if (!string.IsNullOrEmpty(mainLogPath) && File.Exists(mainLogPath))
                {
                    result.LastRunTime = new FileInfo(mainLogPath).LastWriteTime;
                }
                else if (!string.IsNullOrEmpty(errorLogPath) && File.Exists(errorLogPath))
                {
                    result.LastRunTime = new FileInfo(errorLogPath).LastWriteTime;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Status = BatchStatus.Error;
                result.StatusMessage = $"Error analyzing log files: {ex.Message}";
                result.Issues.Add(new LogIssue
                {
                    Type = IssueType.Error,
                    Message = $"Failed to analyze log files: {ex.Message}",
                    LineNumber = 0,
                    FileName = Path.GetFileName(mainLogPath ?? errorLogPath ?? "Unknown"),
                    Timestamp = DateTime.Now
                });
                return result;
            }
        }

        private List<string> DiscoverLogFiles(string directory)
        {
            var logFiles = new List<string>();

            if (!Directory.Exists(directory))
                return logFiles;

            try
            {
                // Common log file patterns
                var patterns = new[] { "*.log", "*.logs", "*.txt" };

                foreach (var pattern in patterns)
                {
                    var files = Directory.GetFiles(directory, pattern)
                        .Where(f => IsLikelyLogFile(f))
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .ToList();

                    logFiles.AddRange(files);
                }

                return logFiles.Distinct().ToList();
            }
            catch
            {
                return logFiles;
            }
        }

        private bool IsLikelyLogFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath).ToLowerInvariant();

            // Common log file indicators
            var logIndicators = new[]
            {
                "log", "error", "batch", "output", "trace",
                "debug", "info", "warn", "exception", "audit"
            };

            return logIndicators.Any(indicator => fileName.Contains(indicator));
        }

        private LogAnalysisResult AnalyzeSingleLogFile(string logFilePath)
        {
            var result = new LogAnalysisResult
            {
                Status = BatchStatus.Unknown,
                StatusMessage = "Unable to analyze log file",
                LastRunTime = DateTime.MinValue,
                DiscoveredLogFiles = new List<string> { logFilePath }
            };

            if (!File.Exists(logFilePath))
                return result;

            try
            {
                var fileInfo = new FileInfo(logFilePath);
                var lastLines = GetLastLinesFromFile(logFilePath, 20);

                // Analyze content for status indicators
                var content = string.Join("\n", lastLines).ToLowerInvariant();

                if (content.Contains("error") || content.Contains("exception") || content.Contains("failed"))
                {
                    result.Status = BatchStatus.Error;
                    result.StatusMessage = $"Errors detected in {Path.GetFileName(logFilePath)}";
                }
                else if (content.Contains("success") || content.Contains("completed") || content.Contains("finished"))
                {
                    result.Status = BatchStatus.Success;
                    result.StatusMessage = $"Success detected in {Path.GetFileName(logFilePath)}";
                }
                else if (content.Contains("running") || content.Contains("processing") || content.Contains("started"))
                {
                    result.Status = BatchStatus.Running;
                    result.StatusMessage = $"Running detected in {Path.GetFileName(logFilePath)}";
                }
                else
                {
                    result.Status = BatchStatus.Unknown;
                    result.StatusMessage = $"Status unclear from {Path.GetFileName(logFilePath)}";
                }

                // Try to extract last run time from file modification or content
                result.LastRunTime = fileInfo.LastWriteTime;

                return result;
            }
            catch (Exception ex)
            {
                result.Status = BatchStatus.Error;
                result.StatusMessage = $"Error reading {Path.GetFileName(logFilePath)}: {ex.Message}";
                return result;
            }
        }

        private LogAnalysisResult AnalyzeSingleLogFileWithIssues(string logFilePath)
        {
            var result = new LogAnalysisResult
            {
                Status = BatchStatus.Unknown,
                StatusMessage = "Unable to analyze log file",
                LastRunTime = DateTime.MinValue,
                DiscoveredLogFiles = new List<string> { logFilePath },
                Issues = new List<LogIssue>()
            };

            if (!File.Exists(logFilePath))
                return result;

            try
            {
                var fileInfo = new FileInfo(logFilePath);
                result.LastRunTime = fileInfo.LastWriteTime;

                var fileName = Path.GetFileName(logFilePath);

                // Read only last 5000 lines for performance
                var lines = ReadLastLines(logFilePath, 5000);

                if (lines.Length == 0)
                {
                    result.Status = BatchStatus.Unknown;
                    result.StatusMessage = "Log file is empty";
                    return result;
                }

                var errorCount = 0;
                var warningCount = 0;
                var infoCount = 0;

                // Analyze each line for issues - optimized for performance
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var lineNumber = i + 1;
                    var lowerLine = line.ToLowerInvariant();

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Try to extract timestamp from line (format: 2024-04-18 02:44:00,907)
                    var timestamp = ExtractTimestampFromLine(line);

                    // Detect errors - Enhanced patterns for your log format
                    if (lowerLine.Contains("] error ") || lowerLine.Contains("[error]") ||
                        lowerLine.Contains("error") || lowerLine.Contains("failed") ||
                        lowerLine.Contains("fatal") || lowerLine.Contains("critical") ||
                        lowerLine.Contains("access is denied"))
                    {
                        result.Issues.Add(new LogIssue
                        {
                            Type = IssueType.Error,
                            Message = line.Trim(),
                            LineNumber = lineNumber,
                            FileName = fileName,
                            Timestamp = timestamp
                        });
                        errorCount++;
                    }
                    // Detect warnings - Enhanced patterns
                    else if (lowerLine.Contains("] warn ") || lowerLine.Contains("warning") ||
                             lowerLine.Contains("warn") || lowerLine.Contains("caution") ||
                             lowerLine.Contains(" alert "))
                    {
                        result.Issues.Add(new LogIssue
                        {
                            Type = IssueType.Warning,
                            Message = line.Trim(),
                            LineNumber = lineNumber,
                            FileName = fileName,
                            Timestamp = timestamp
                        });
                        warningCount++;
                    }
                    // Detect important info - Enhanced patterns
                    else if (lowerLine.Contains("] info ") || lowerLine.Contains("info") ||
                             lowerLine.Contains("successfully") || lowerLine.Contains("completed") ||
                             lowerLine.Contains("started") || lowerLine.Contains("finished"))
                    {
                        result.Issues.Add(new LogIssue
                        {
                            Type = IssueType.Info,
                            Message = line.Trim(),
                            LineNumber = lineNumber,
                            FileName = fileName,
                            Timestamp = timestamp
                        });
                        infoCount++;
                    }
                }

                // Determine overall status based on issues found
                if (errorCount > 0)
                {
                    result.Status = BatchStatus.Error;
                }
                else if (warningCount > 0)
                {
                    result.Status = BatchStatus.Warning;
                }
                else if (infoCount > 0)
                {
                    result.Status = BatchStatus.Success;
                }
                else
                {
                    result.Status = BatchStatus.Unknown;
                }

                result.LastRunTime = fileInfo.LastWriteTime;
                return result;
            }
            catch (Exception ex)
            {
                result.Status = BatchStatus.Error;
                result.StatusMessage = $"Error reading {Path.GetFileName(logFilePath)}: {ex.Message}";
                result.Issues.Add(new LogIssue
                {
                    Type = IssueType.Error,
                    Message = $"Failed to read log file: {ex.Message}",
                    LineNumber = 0,
                    FileName = Path.GetFileName(logFilePath),
                    Timestamp = DateTime.Now
                });
                return result;
            }
        }

        private LogAnalysisResult AnalyzeSingleLogFileWithIssues(string logFilePath, DateTime filterDate)
        {
            var result = new LogAnalysisResult
            {
                Status = BatchStatus.Unknown,
                StatusMessage = "Unable to analyze log file",
                LastRunTime = DateTime.MinValue,
                DiscoveredLogFiles = new List<string> { logFilePath },
                Issues = new List<LogIssue>()
            };

            if (!File.Exists(logFilePath))
                return result;

            try
            {
                var fileInfo = new FileInfo(logFilePath);
                result.LastRunTime = fileInfo.LastWriteTime;

                var fileName = Path.GetFileName(logFilePath);

                // Read only last 5000 lines for performance
                var lines = ReadLastLines(logFilePath, 5000);

                if (lines.Length == 0)
                {
                    result.Status = BatchStatus.Unknown;
                    result.StatusMessage = "Log file is empty";
                    return result;
                }

                var errorCount = 0;
                var warningCount = 0;
                var infoCount = 0;

                // Analyze each line for issues - optimized for performance
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var lineNumber = i + 1;
                    var lowerLine = line.ToLowerInvariant();

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Try to extract timestamp from line (format: 2024-04-18 02:44:00,907)
                    var timestamp = ExtractTimestampFromLine(line);

                    // Filter by date - only include entries from the filter date or later
                    if (timestamp.Date < filterDate.Date)
                        continue;

                    // Detect errors - Enhanced patterns for your log format
                    if (lowerLine.Contains("] error ") || lowerLine.Contains("[error]")
                        || lowerLine.Contains("failed") || lowerLine.Contains("error") ||
                        lowerLine.Contains("fatal") || lowerLine.Contains("critical") ||
                        lowerLine.Contains("access is denied"))
                    {
                        result.Issues.Add(new LogIssue
                        {
                            Type = IssueType.Error,
                            Message = line.Trim(),
                            LineNumber = lineNumber,
                            FileName = fileName,
                            Timestamp = timestamp
                        });
                        errorCount++;
                    }
                    // Detect warnings - Enhanced patterns
                    else if (lowerLine.Contains("] warn ") || lowerLine.Contains("warning") ||
                             lowerLine.Contains("warn") || lowerLine.Contains("caution") || lowerLine.Contains("exception")
                             || lowerLine.Contains(" alert "))
                    {
                        result.Issues.Add(new LogIssue
                        {
                            Type = IssueType.Warning,
                            Message = line.Trim(),
                            LineNumber = lineNumber,
                            FileName = fileName,
                            Timestamp = timestamp
                        });
                        warningCount++;
                    }
                    // Detect important info - Enhanced patterns
                    else if (lowerLine.Contains("] info ") || lowerLine.Contains("info") ||
                             lowerLine.Contains("successfully") || lowerLine.Contains("completed") ||
                             lowerLine.Contains("started") || lowerLine.Contains("finished"))
                    {
                        result.Issues.Add(new LogIssue
                        {
                            Type = IssueType.Info,
                            Message = line.Trim(),
                            LineNumber = lineNumber,
                            FileName = fileName,
                            Timestamp = timestamp
                        });
                        infoCount++;
                    }
                }

                // Determine overall status based on issues found
                if (errorCount > 0)
                {
                    result.Status = BatchStatus.Error;
                }
                else if (warningCount > 0)
                {
                    result.Status = BatchStatus.Warning;
                }
                else if (infoCount > 0)
                {
                    result.Status = BatchStatus.Success;
                }
                else
                {
                    result.Status = BatchStatus.Unknown;
                }

                result.LastRunTime = fileInfo.LastWriteTime;
                return result;
            }
            catch (Exception ex)
            {
                result.Status = BatchStatus.Error;
                result.StatusMessage = $"Error reading {Path.GetFileName(logFilePath)}: {ex.Message}";
                result.Issues.Add(new LogIssue
                {
                    Type = IssueType.Error,
                    Message = $"Failed to read log file: {ex.Message}",
                    LineNumber = 0,
                    FileName = Path.GetFileName(logFilePath),
                    Timestamp = DateTime.Now
                });
                return result;
            }
        }

        private List<string> GetLastLinesFromFile(string filePath, int lineCount)
        {
            var lines = new List<string>();

            try
            {
                var allLines = File.ReadAllLines(filePath);
                var startIndex = Math.Max(0, allLines.Length - lineCount);
                lines.AddRange(allLines.Skip(startIndex));
            }
            catch
            {
                // Return empty list if file can't be read
            }

            return lines;
        }

        // Fast method to read only the last N lines of a file
        private string[] ReadLastLines(string filePath, int maxLines)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                // For small files, just read all lines
                if (fileInfo.Length < 1024 * 1024) // 1MB
                {
                    return File.ReadAllLines(filePath);
                }

                // For large files, read from the end
                var lines = new List<string>();
                using (var reader = new StreamReader(filePath))
                {
                    var allLines = new List<string>();
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        allLines.Add(line);
                    }

                    // Return last N lines
                    int startIndex = Math.Max(0, allLines.Count - maxLines);
                    return allLines.Skip(startIndex).ToArray();
                }
            }
            catch
            {
                return new string[0];
            }
        }

        // Enhanced helper method to extract timestamp from log line - now supports bracket format
        private DateTime ExtractTimestampFromLine(string line)
        {
            try
            {
                // Pattern 1: Bracket format [2025-07-15 02:50:00]
                if (line.StartsWith("[") && line.Length >= 21)
                {
                    var closingBracket = line.IndexOf(']');
                    if (closingBracket > 0)
                    {
                        var timestampPart = line.Substring(1, closingBracket - 1); // Remove [ and ]

                        // Try to parse bracket timestamp format
                        if (DateTime.TryParseExact(timestampPart, "yyyy-MM-dd HH:mm:ss",
                            null, System.Globalization.DateTimeStyles.None, out DateTime bracketResult))
                        {
                            return bracketResult;
                        }

                        // Try with milliseconds in bracket format
                        if (timestampPart.Length >= 23 && DateTime.TryParseExact(timestampPart, "yyyy-MM-dd HH:mm:ss,fff",
                            null, System.Globalization.DateTimeStyles.None, out DateTime bracketResultMs))
                        {
                            return bracketResultMs;
                        }

                        // Generic parse attempt for bracket content
                        if (DateTime.TryParse(timestampPart, out DateTime bracketGeneric))
                        {
                            return bracketGeneric;
                        }
                    }
                }

                // Pattern 2: Original format: 2024-04-18 02:44:00,907 (backward compatibility)
                if (line.Length >= 23)
                {
                    var timestampPart = line.Substring(0, 23);

                    // Try to parse various timestamp formats
                    if (DateTime.TryParseExact(timestampPart, "yyyy-MM-dd HH:mm:ss,fff",
                        null, System.Globalization.DateTimeStyles.None, out DateTime result))
                    {
                        return result;
                    }

                    if (DateTime.TryParseExact(timestampPart.Substring(0, 19), "yyyy-MM-dd HH:mm:ss",
                        null, System.Globalization.DateTimeStyles.None, out result))
                    {
                        return result;
                    }
                }

                // Pattern 3: Fallback - try to find any date-time pattern in the line
                var words = line.Split(' ');
                for (int i = 0; i < words.Length - 1; i++)
                {
                    var combined = $"{words[i]} {words[i + 1]}";

                    // Clean up brackets and other characters
                    combined = combined.Replace("[", "").Replace("]", "").Replace("-", " ");

                    if (DateTime.TryParse(combined, out DateTime parsed))
                    {
                        return parsed;
                    }
                }

                // Pattern 4: Try parsing individual words that might contain dates
                foreach (var word in words)
                {
                    var cleanWord = word.Replace("[", "").Replace("]", "").Replace(",", "");
                    if (DateTime.TryParse(cleanWord, out DateTime wordParsed))
                    {
                        return wordParsed;
                    }
                }
            }
            catch
            {
                // If parsing fails, use current time
            }

            return DateTime.Now;
        }

        // Additional utility methods for compatibility
        public string GetDailyZipPath(string logFilePath)
        {
            if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
                return string.Empty;

            var directory = Path.GetDirectoryName(logFilePath);
            var fileName = Path.GetFileNameWithoutExtension(logFilePath);
            var yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");

            return Path.Combine(directory ?? "", $"{fileName}_{yesterday}.zip");
        }

        public bool IsLogFileRecentlyUpdated(string logFilePath, int hourThreshold)
        {
            if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
                return false;

            try
            {
                var lastWriteTime = File.GetLastWriteTime(logFilePath);
                var threshold = DateTime.Now.AddHours(-hourThreshold);
                return lastWriteTime > threshold;
            }
            catch
            {
                return false;
            }
        }
    }

    // Result class for log analysis
    public class LogAnalysisResult
    {
        public BatchStatus Status { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime LastRunTime { get; set; }
        public List<string> DiscoveredLogFiles { get; set; } = new List<string>();
        public List<LogIssue> Issues { get; set; } = new List<LogIssue>();
    }
}