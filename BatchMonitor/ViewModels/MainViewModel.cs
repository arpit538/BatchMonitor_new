using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using BatchMonitor.Models;
using BatchMonitor.Services;

namespace BatchMonitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly BatchService _batchService;
        private readonly DispatcherTimer _refreshTimer;
        private BatchItem? _selectedBatch;
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private bool _autoRefreshEnabled = true;
        private int _refreshIntervalSeconds = 30;
        private DateTime _filterDate;

        public MainViewModel()
        {
            _batchService = new BatchService();
            Batches = new ObservableCollection<BatchItem>();
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(_refreshIntervalSeconds);
            _refreshTimer.Tick += RefreshTimer_Tick;

            // Commands
            LoadBatchesCommand = new RelayCommand(LoadBatches);
            AddBatchCommand = new RelayCommand(AddBatch);
            RemoveBatchCommand = new RelayCommand(RemoveBatch, () => SelectedBatch != null);
            RefreshCommand = new RelayCommand(RefreshBatches);
            ScheduleBatchCommand = new RelayCommand(ScheduleBatch, () => SelectedBatch != null);
            ViewLogsCommand = new RelayCommand(ViewLogs, () => SelectedBatch != null);
            ViewMainLogCommand = new RelayCommand(() => ViewMainLog(SelectedBatch));
            ViewErrorLogCommand = new RelayCommand(() => ViewErrorLog(SelectedBatch));
            ViewConfigCommand = new RelayCommand(() => ViewConfig(SelectedBatch));
            ToggleAutoRefreshCommand = new RelayCommand(ToggleAutoRefresh);
            SetFilterDateCommand = new RelayCommand(SetFilterDate);

            // Initialize with today's date as filter
            FilterDate = DateTime.Today;

            // Load batches and their status using today's date
            LoadBatches();
            RefreshBatches();

            if (_autoRefreshEnabled)
            {
                _refreshTimer.Start();
            }
        }

        public ObservableCollection<BatchItem> Batches { get; }

        public BatchItem? SelectedBatch
        {
            get => _selectedBatch;
            set
            {
                _selectedBatch = value;
                OnPropertyChanged();
                ((RelayCommand)RemoveBatchCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ScheduleBatchCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ViewLogsCommand).RaiseCanExecuteChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set
            {
                _autoRefreshEnabled = value;
                OnPropertyChanged();

                if (_autoRefreshEnabled)
                {
                    _refreshTimer.Start();
                    StatusMessage = $"Auto-refresh enabled (every {_refreshIntervalSeconds} seconds)";
                }
                else
                {
                    _refreshTimer.Stop();
                    StatusMessage = "Auto-refresh disabled";
                }
            }
        }

        public int RefreshIntervalSeconds
        {
            get => _refreshIntervalSeconds;
            set
            {
                _refreshIntervalSeconds = value;
                OnPropertyChanged();

                _refreshTimer.Interval = TimeSpan.FromSeconds(_refreshIntervalSeconds);

                if (_autoRefreshEnabled)
                {
                    StatusMessage = $"Auto-refresh interval set to {_refreshIntervalSeconds} seconds";
                }
            }
        }

        public DateTime FilterDate
        {
            get => _filterDate;
            set
            {
                _filterDate = value;
                OnPropertyChanged();
                Batches.Clear();
                LoadBatches();
            }
        }
        

        public ICommand LoadBatchesCommand { get; }
        public ICommand AddBatchCommand { get; }
        public ICommand RemoveBatchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ScheduleBatchCommand { get; }
        public ICommand ViewLogsCommand { get; }
        public ICommand ViewMainLogCommand { get; }
        public ICommand ViewErrorLogCommand { get; }
        public ICommand ViewConfigCommand { get; }
        public ICommand ToggleAutoRefreshCommand { get; }
        public ICommand SetFilterDateCommand { get; }

        // Expose BatchService for external access
        public BatchService BatchService => _batchService;

        private static readonly SemaphoreSlim _statusUpdateSemaphore = new SemaphoreSlim(4); // Limit to 4 concurrent updates

        private async void LoadBatches()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading batches...";

                var batches = await _batchService.LoadBatchesAsync();

                // Update Batches collection on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Batches.Clear();
                    foreach (var batch in batches)
                    {
                        Batches.Add(batch);
                    }
                });

                StatusMessage = $"Loaded {batches.Count} batches (Filter: {FilterDate:d})";

                // Update all batch statuses in parallel, but collect tasks
                var updateTasks = Batches.Select(batch => Task.Run(async () =>
                {
                    await _statusUpdateSemaphore.WaitAsync();
                    try
                    {
                        var analysisResult = await Task.Run(() => _batchService.AnalyzeBatchForViewModel(batch, FilterDate));
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            batch.Status = analysisResult.Status;
                            batch.StatusMessage = analysisResult.StatusMessage;
                            batch.LastRun = analysisResult.LastRun;
                            batch.DiscoveredLogFiles.Clear();
                            if (analysisResult.DiscoveredLogFiles != null)
                            {
                                foreach (var log in analysisResult.DiscoveredLogFiles)
                                    batch.DiscoveredLogFiles.Add(log);
                            }
                            batch.Issues.Clear();
                            if (analysisResult.Issues != null)
                            {
                                foreach (var issue in analysisResult.Issues)
                                    batch.Issues.Add(issue);
                            }
                            batch.IsScheduled = analysisResult.IsScheduled;
                            batch.NextRun = analysisResult.NextRun;
                            StatusMessage = $"Batch '{batch.Name}' status updated for {FilterDate:d}";
                            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Batches);
                            view.Refresh();
                        });
                    }
                    finally
                    {
                        _statusUpdateSemaphore.Release();
                    }
                })).ToList();

                await Task.WhenAll(updateTasks);

                // After all batch statuses are updated, send the summary email
                try
                {
                    await _batchService.SendBatchReportEmailAsync(Batches.ToList(), FilterDate);
                    StatusMessage = $"Batch report email sent for {FilterDate:d}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error sending batch report email: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading batches: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddBatch()
        {
            var dialog = new Views.AddBatchDialog();
            if (dialog.ShowDialog() == true)
            {
                var batchType = dialog.BatchTypeComboBox.SelectedItem is ComboBoxItem selectedItem &&
                               selectedItem.Tag?.ToString() == "Hourly" ? BatchType.Hourly : BatchType.FixedTime;

                var newBatch = new BatchItem
                {
                    Name = dialog.BatchName,
                    LogFilePath = dialog.LogFilePath,
                    ErrorLogFilePath = dialog.ErrorLogFilePath,
                    CustomLogFilePath = dialog.CustomLogFilePath,
                    ConfigFilePath = dialog.ConfigFilePath,
                    ExecutablePath = dialog.ExecutablePath,
                    BatchType = batchType
                };

                _batchService.UpdateBatchStatusAsync(newBatch);
                Batches.Add(newBatch);
                SaveBatches();
                StatusMessage = $"Added batch: {newBatch.Name}";
            }
        }

        private void RemoveBatch()
        {
            var batchToRemove = SelectedBatch;
            if (batchToRemove == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to remove batch '{batchToRemove.Name}'?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (batchToRemove.IsScheduled)
                    {
                        _batchService.DeleteSchedule(batchToRemove);
                    }

                    Batches.Remove(batchToRemove);
                    SaveBatches();
                    StatusMessage = $"Removed batch: {batchToRemove.Name}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error removing batch: {ex.Message}";
                }
            }
        }

        private void RefreshBatches()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Refreshing batches...";

                foreach (var batch in Batches)
                {
                    _ = Task.Run(async () =>
                    {
                        await _statusUpdateSemaphore.WaitAsync();
                        try
                        {
                            await _batchService.UpdateBatchStatusAsync(batch, FilterDate);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // No need to replace the batch, just update properties (INotifyPropertyChanged will update UI)
                                StatusMessage = $"Batch '{batch.Name}' status updated for {FilterDate:d}";
                            });
                        }
                        finally
                        {
                            _statusUpdateSemaphore.Release();
                        }
                    });
                }

                StatusMessage = $"Batches refresh started (Filter: {FilterDate:d})";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing batches: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ViewLogs()
        {
            if (SelectedBatch == null) return;

            var dialog = new Views.LogViewerDialog(SelectedBatch, _batchService);
            dialog.Show();
        }

        private void ViewMainLog(BatchItem? batch)
        {
            if (batch == null) return;

            try
            {
                var content = _batchService.ReadLogFile(batch.LogFilePath);
                var dialog = new Views.LogViewerDialog(batch.Name + " - Main Log", content);
                dialog.Show();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error viewing main log: {ex.Message}";
            }
        }

        private void ViewErrorLog(BatchItem? batch)
        {
            if (batch == null) return;

            try
            {
                var content = _batchService.ReadLogFile(batch.ErrorLogFilePath);
                var dialog = new Views.LogViewerDialog(batch.Name + " - Error Log", content);
                dialog.Show();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error viewing error log: {ex.Message}";
            }
        }

        private void ViewConfig(BatchItem? batch)
        {
            if (batch == null) return;

            try
            {
                var dialog = new Views.LogViewerDialog(batch, _batchService, true);
                dialog.Show();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error viewing config file: {ex.Message}";
            }
        }

        private void ToggleAutoRefresh()
        {
            AutoRefreshEnabled = !AutoRefreshEnabled;
        }

        private void SetFilterDate()
        {
            var dialog = new Views.ScheduleDialog();
            if (dialog.ShowDialog() == true)
            {
                FilterDate = dialog.SelectedDateTime;
                // FilterDate property setter will handle refreshing
            }
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isLoading && _autoRefreshEnabled)
            {
                RefreshBatches();
            }
        }

        private void SaveBatches()
        {
            try
            {
                var batchList = new System.Collections.Generic.List<BatchItem>(Batches);
                _batchService.SaveBatches(batchList);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving batches: {ex.Message}";
            }
        }

        private void ScheduleBatch()
        {
            var batchToSchedule = SelectedBatch;
            if (batchToSchedule == null) return;

            var dialog = new Views.ChangeScheduleDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "Scheduling batch...";

                    // Schedule the batch
                    _batchService.ScheduleBatch(batchToSchedule, dialog.SelectedDateTime);

                    // Verify the scheduling worked by checking the task scheduler
                    var isActuallyScheduled = _batchService.IsScheduled(batchToSchedule.Name);
                    var actualNextRun = _batchService.GetNextRunTime(batchToSchedule.Name);

                    if (isActuallyScheduled && actualNextRun.HasValue)
                    {
                        // Update the batch with actual values from task scheduler
                        batchToSchedule.IsScheduled = true;
                        batchToSchedule.NextRun = actualNextRun.Value;
                        StatusMessage = $"Successfully scheduled batch: {batchToSchedule.Name} at {actualNextRun.Value:yyyy-MM-dd HH:mm}";
                    }
                    else
                    {
                        // Scheduling failed, reset the batch status
                        batchToSchedule.IsScheduled = false;
                        batchToSchedule.NextRun = DateTime.MinValue;
                        StatusMessage = $"Failed to schedule batch: {batchToSchedule.Name}. Please check permissions.";
                    }

                    SaveBatches();
                }
                catch (Exception ex)
                {
                    if (batchToSchedule != null)
                    {
                        batchToSchedule.IsScheduled = false;
                        batchToSchedule.NextRun = DateTime.MinValue;
                    }
                    StatusMessage = $"Error scheduling batch: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
