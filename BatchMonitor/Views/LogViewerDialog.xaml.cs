using System;
using System.IO;
using System.Windows;
using BatchMonitor.Models;
using BatchMonitor.Services;

namespace BatchMonitor.Views
{
    public partial class LogViewerDialog : Window
    {
        private readonly BatchItem _batch;
        private readonly BatchService _batchService;

        public LogViewerDialog(BatchItem batch, BatchService batchService)
        {
            InitializeComponent();
            _batch = batch;
            _batchService = batchService;
            
            // Hide config tab for logs view
            ConfigTab.Visibility = Visibility.Collapsed;
            
            // Select main log tab by default
            MainLogTab.IsSelected = true;
            
            LoadLogFiles();
        }

        // Constructor for config-only view
        public LogViewerDialog(BatchItem batch, BatchService batchService, bool isConfigView)
        {
            InitializeComponent();
            _batch = batch;
            _batchService = batchService;
            
            if (isConfigView)
            {
                // Hide log tabs for config view
                MainLogTab.Visibility = Visibility.Collapsed;
                ErrorLogTab.Visibility = Visibility.Collapsed;
                
                // Select config tab and load data immediately
                ConfigTab.IsSelected = true;
                LoadConfigFile();
            }
        }

        private void LoadLogFiles()
        {
            try
            {
                StatusTextBlock.Text = "Loading log files...";
                
                // Load all available log files based on what the user provided
                bool hasMainLog = !string.IsNullOrEmpty(_batch.LogFilePath) && File.Exists(_batch.LogFilePath);
                bool hasErrorLog = !string.IsNullOrEmpty(_batch.ErrorLogFilePath) && File.Exists(_batch.ErrorLogFilePath);
                bool hasCustomLog = !string.IsNullOrEmpty(_batch.CustomLogFilePath) && File.Exists(_batch.CustomLogFilePath);

                // Show/hide tabs based on available files
                MainLogTab.Visibility = hasMainLog ? Visibility.Visible : Visibility.Collapsed;
                ErrorLogTab.Visibility = hasErrorLog ? Visibility.Visible : Visibility.Collapsed;
                
                // If custom log exists, show it in main log tab with custom name
                if (hasCustomLog)
                {
                    MainLogTab.Visibility = Visibility.Visible;
                    MainLogTab.Header = $"Custom Log ({System.IO.Path.GetFileName(_batch.CustomLogFilePath)})";
                    var customLogContent = _batchService.ReadLogFile(_batch.CustomLogFilePath);
                    MainLogTextBox.Text = customLogContent;
                }
                else if (hasMainLog)
                {
                    MainLogTab.Header = "Main Log (Batch.logs)";
                    var mainLogContent = _batchService.ReadLogFile(_batch.LogFilePath);
                    MainLogTextBox.Text = mainLogContent;
                }

                if (hasErrorLog)
                {
                    var errorLogContent = _batchService.ReadLogFile(_batch.ErrorLogFilePath);
                    ErrorLogTextBox.Text = errorLogContent;
                    ErrorLogTab.Header = "Error Log (Error.logs)";
                }

                // Select appropriate default tab
                if (hasCustomLog)
                {
                    MainLogTab.IsSelected = true;
                }
                else if (hasMainLog)
                {
                    MainLogTab.IsSelected = true;
                }
                else if (hasErrorLog)
                {
                    ErrorLogTab.IsSelected = true;
                }
                
                StatusTextBlock.Text = "Log files loaded successfully";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error loading log files: {ex.Message}";
            }
        }

        private void LoadConfigFile()
        {
            try
            {
                StatusTextBlock.Text = "Loading config file...";
                
                // Load config file
                var configContent = _batchService.ReadConfigFile(_batch.ConfigFilePath);
                ConfigTextEditor.Text = configContent; // Use AvalonEdit control
                // Optionally set syntax highlighting (should be set in XAML, but can be set here too)
                // ConfigTextEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("XML");
                StatusTextBlock.Text = "Config file loaded successfully";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error loading config file: {ex.Message}";
            }
        }

        public LogViewerDialog(string title, string content)
        {
            InitializeComponent();
            _batch = null!;
            _batchService = null!;
            // Check if it's a config file or log file based on title
            if (title.Contains("Config"))
            {
                ConfigTextEditor.Text = content; // Use AvalonEdit control
                // Optionally set syntax highlighting (should be set in XAML, but can be set here too)
                // ConfigTextEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("XML");
            }
            else
            {
                MainLogTextBox.Text = content;
            }
            StatusTextBlock.Text = "Content loaded";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLogFiles();
        }
    }
}
