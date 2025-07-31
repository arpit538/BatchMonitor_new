using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BatchMonitor.Models;
using BatchMonitor.Services;

namespace BatchMonitor.Views
{
    public partial class IssueDetailsDialog : Window
    {
        private readonly BatchItem _batch;
        private readonly BatchService _batchService;
        private readonly DateTime _filterDate;

        public IssueDetailsDialog(BatchItem batch, BatchService batchService, DateTime filterDate)
        {
            InitializeComponent();
            _batch = batch;
            _batchService = batchService;
            _filterDate = filterDate.Date;
            LoadIssues();
        }

        private void LoadIssues()
        {
            // Filter issues by the specific filter date
            var issuesForDate = _batch.Issues.Where(i => i.Timestamp.Date == _filterDate).ToList();

            if (!issuesForDate.Any())
            {
                IssuesDataGrid.ItemsSource = null;
                return;
            }

            // Priority logic: Error > Warning > Info
            var errorIssues = issuesForDate.Where(i => i.Type == IssueType.Error).ToList();
            var warningIssues = issuesForDate.Where(i => i.Type == IssueType.Warning).ToList();
            var infoIssues = issuesForDate.Where(i => i.Type == IssueType.Info).ToList();

            // Show issues based on priority - highest priority type only
            if (errorIssues.Any())
            {
                IssuesDataGrid.ItemsSource = errorIssues.OrderByDescending(i => i.Timestamp).ThenByDescending(i => i.LineNumber);
            }
            else if (warningIssues.Any())
            {
                IssuesDataGrid.ItemsSource = warningIssues.OrderByDescending(i => i.Timestamp).ThenByDescending(i => i.LineNumber);
            }
            else if (infoIssues.Any())
            {
                IssuesDataGrid.ItemsSource = infoIssues.OrderByDescending(i => i.Timestamp).ThenByDescending(i => i.LineNumber);
            }
            else
            {
                IssuesDataGrid.ItemsSource = null;
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Refresh batch status to get latest issues with the filter date
                _batchService.UpdateBatchStatus(_batch, _filterDate);
                LoadIssues();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing batch status: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowAllIssues_Click(object sender, RoutedEventArgs e)
        {
            // Optional: Show all issues for the date regardless of priority
            var allIssuesForDate = _batch.Issues
                .Where(i => i.Timestamp.Date == _filterDate)
                .OrderBy(i => i.Type)
                .ThenByDescending(i => i.Timestamp)
                .ThenByDescending(i => i.LineNumber)
                .ToList();

            IssuesDataGrid.ItemsSource = allIssuesForDate;
        }

        private void FilterByType_Click(object sender, RoutedEventArgs e)
        {
            // Optional: Filter by specific issue type
            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag is string issueTypeString)
            {
                if (Enum.TryParse<IssueType>(issueTypeString, out var issueType))
                {
                    var filteredIssues = _batch.Issues
                        .Where(i => i.Timestamp.Date == _filterDate && i.Type == issueType)
                        .OrderByDescending(i => i.Timestamp)
                        .ThenByDescending(i => i.LineNumber)
                        .ToList();

                    IssuesDataGrid.ItemsSource = filteredIssues;
                }
            }
        }
    }
}