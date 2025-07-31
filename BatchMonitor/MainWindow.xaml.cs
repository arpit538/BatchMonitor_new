using System;
using System.Windows;
using BatchMonitor.ViewModels;

namespace BatchMonitor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up resources
            if (DataContext is MainViewModel viewModel)
            {
                // Add cleanup logic if needed
            }
            base.OnClosed(e);
        }

        private void StatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is BatchMonitor.Models.BatchItem batch)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    // Show issue details dialog with filter date
                    var issueDialog = new Views.IssueDetailsDialog(batch, viewModel.BatchService, viewModel.FilterDate);
                    issueDialog.Owner = this;
                    issueDialog.ShowDialog();
                }
            }
        }
    }
}
