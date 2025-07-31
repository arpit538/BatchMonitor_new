using System.Windows;
using Microsoft.Win32;
using BatchMonitor.Models;
using System;
using System.IO;

namespace BatchMonitor.Views
{
    public partial class AddBatchDialog : Window
    {
        public string BatchName => BatchNameTextBox.Text;
        public string LogFilePath => LogFilePathTextBox.Text;
        public string ErrorLogFilePath => ErrorLogFilePathTextBox.Text;
        public string CustomLogFilePath => CustomLogFilePathTextBox.Text;
        public string ConfigFilePath => ConfigFilePathTextBox.Text;
        public string ExecutablePath => ExecutablePathTextBox.Text;
        public BatchType BatchType => 
            BatchTypeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem && 
            selectedItem.Tag?.ToString() == "Hourly" ? BatchType.Hourly : BatchType.FixedTime;

        public AddBatchDialog()
        {
            InitializeComponent();
            BatchTypeComboBox.SelectedIndex = 0; // Default to FixedTime
        }

        private void BrowseLogFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Main Log File (Batch.logs)",
                Filter = "Log Files (*.log;*.logs)|*.log;*.logs|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                InitialDirectory = Environment.CurrentDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                LogFilePathTextBox.Text = dialog.FileName;
                
                var directory = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(directory))
                {
                    // Auto-suggest error log file path
                    if (string.IsNullOrEmpty(ErrorLogFilePathTextBox.Text))
                    {
                        var errorLogPath = Path.Combine(directory, "Error.logs");
                        if (File.Exists(errorLogPath))
                        {
                            ErrorLogFilePathTextBox.Text = errorLogPath;
                        }
                    }
                    
                    // Auto-suggest StartServices.exe
                    if (string.IsNullOrEmpty(ExecutablePathTextBox.Text))
                    {
                        var startServicesPath = Path.Combine(directory, "StartServices.exe");
                        if (File.Exists(startServicesPath))
                        {
                            ExecutablePathTextBox.Text = startServicesPath;
                        }
                    }
                }
            }
        }

        private void BrowseErrorLogFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Error Log File (Error.logs)",
                Filter = "Log Files (*.log;*.logs)|*.log;*.logs|Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                ErrorLogFilePathTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseConfigFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Config File",
                Filter = "Config Files (*.config)|*.config|XML Files (*.xml)|*.xml|JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                ConfigFilePathTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseExecutableButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Executable File (StartServices.exe)",
                Filter = "Executable Files (*.exe;*.bat;*.cmd;*.ps1)|*.exe;*.bat;*.cmd;*.ps1|All Files (*.*)|*.*",
                InitialDirectory = string.IsNullOrEmpty(LogFilePathTextBox.Text) ? 
                    Environment.CurrentDirectory : 
                    Path.GetDirectoryName(LogFilePathTextBox.Text) ?? Environment.CurrentDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                ExecutablePathTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseCustomLogFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Custom Log File (file.txt, etc.)",
                Filter = "Text Files (*.txt)|*.txt|Log Files (*.log;*.logs)|*.log;*.logs|All Files (*.*)|*.*",
                InitialDirectory = Environment.CurrentDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                CustomLogFilePathTextBox.Text = dialog.FileName;
                
                var directory = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(directory))
                {
                    // Auto-suggest StartServices.exe
                    if (string.IsNullOrEmpty(ExecutablePathTextBox.Text))
                    {
                        var startServicesPath = Path.Combine(directory, "StartServices.exe");
                        if (File.Exists(startServicesPath))
                        {
                            ExecutablePathTextBox.Text = startServicesPath;
                        }
                    }
                }
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BatchName))
            {
                MessageBox.Show("Please enter a batch name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // At least one log file path must be provided
            if (string.IsNullOrWhiteSpace(LogFilePath) && string.IsNullOrWhiteSpace(ErrorLogFilePath) && string.IsNullOrWhiteSpace(CustomLogFilePath))
            {
                MessageBox.Show("Please provide at least one log file path (Main Log, Error Log, or Custom Log).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
