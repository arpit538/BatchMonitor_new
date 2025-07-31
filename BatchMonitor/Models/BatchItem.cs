using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BatchMonitor.Models
{
    public enum BatchType
    {
        FixedTime,    // Morning/Evening fixed time, logs overwrite
        Hourly        // Hourly trigger, logs append, daily zip
    }

    public class BatchItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _logFilePath = string.Empty;
        private string _errorLogFilePath = string.Empty;
        private string _configFilePath = string.Empty;
        private string _executablePath = string.Empty;
        private string _customLogFilePath = string.Empty;
        private BatchStatus _status = BatchStatus.Unknown;
        private DateTime _lastRun = DateTime.MinValue;
        private DateTime _nextRun = DateTime.MinValue;
        private string _statusMessage = string.Empty;
        private bool _isScheduled = false;
        private BatchType _batchType = BatchType.FixedTime;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string LogFilePath
        {
            get => _logFilePath;
            set { _logFilePath = value; OnPropertyChanged(); }
        }

        public string ErrorLogFilePath
        {
            get => _errorLogFilePath;
            set { _errorLogFilePath = value; OnPropertyChanged(); }
        }

        public string ConfigFilePath
        {
            get => _configFilePath;
            set { _configFilePath = value; OnPropertyChanged(); }
        }

        public string ExecutablePath
        {
            get => _executablePath;
            set
            {
                _executablePath = value;
                OnPropertyChanged();
            }
        }

        public string CustomLogFilePath
        {
            get => _customLogFilePath;
            set
            {
                _customLogFilePath = value;
                OnPropertyChanged();
            }
        }

        public BatchType BatchType
        {
            get => _batchType;
            set { _batchType = value; OnPropertyChanged(); }
        }

        public BatchStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public DateTime LastRun
        {
            get => _lastRun;
            set { _lastRun = value; OnPropertyChanged(); }
        }

        public DateTime NextRun
        {
            get => _nextRun;
            set { _nextRun = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsScheduled
        {
            get => _isScheduled;
            set { _isScheduled = value; OnPropertyChanged(); }
        }


        // For storing discovered log files
        private ObservableCollection<string> _discoveredLogFiles = new ObservableCollection<string>();
        public ObservableCollection<string> DiscoveredLogFiles
        {
            get => _discoveredLogFiles;
            set
            {
                if (_discoveredLogFiles != value)
                {
                    _discoveredLogFiles = value;
                    OnPropertyChanged();
                }
            }
        }

        // For storing detailed error/warning information
        private ObservableCollection<LogIssue> _issues = new ObservableCollection<LogIssue>();
        public ObservableCollection<LogIssue> Issues
        {
            get => _issues;
            set
            {
                if (_issues != value)
                {
                    _issues = value;
                    OnPropertyChanged();
                }
            }
        }

        // Simple status text for UI display
        public string SimpleStatus => Status switch
        {
            BatchStatus.Success => "Success",
            BatchStatus.Warning => "Warnings", 
            BatchStatus.Error => "Errors",
            BatchStatus.Running => "Running",
            _ => "Unknown"
        };

        // Issue count for display (only show count if there are issues)
        public string IssueCount => Issues.Count > 0 ? $"({Issues.Count})" : "";

        // Combined display text - just show the simple status
        public string StatusDisplayText => SimpleStatus;

        public string StatusColor => Status switch
        {
            BatchStatus.Success => "#4CAF50",
            BatchStatus.Warning => "#FF9800",
            BatchStatus.Error => "#F44336",
            BatchStatus.Running => "#2196F3",
            _ => "#9E9E9E"
        };

        public string StatusIcon => Status switch
        {
            BatchStatus.Success => "CheckCircle",
            BatchStatus.Warning => "AlertCircle",
            BatchStatus.Error => "CloseCircle",
            BatchStatus.Running => "PlayCircle",
            _ => "HelpCircle"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum BatchStatus
    {
        Unknown,
        Success,
        Warning,
        Error,
        Running
    }

    // Class for storing individual log issues
    public class LogIssue
    {
        public IssueType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public enum IssueType
    {
        Error,
        Warning,
        Info
    }
}
