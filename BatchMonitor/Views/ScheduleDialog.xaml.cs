using System;
using System.Linq;
using System.Windows;

namespace BatchMonitor.Views
{
    public partial class ScheduleDialog : Window
    {
        public DateTime SelectedDateTime { get; private set; }
        private readonly bool _isScheduling;

        public ScheduleDialog(bool isScheduling = false, string? title = null)
        {
            InitializeComponent();
            _isScheduling = isScheduling;
            
            if (!string.IsNullOrEmpty(title))
                Title = title;

            
            ConfirmButton.Content = isScheduling ? "Schedule" : "Apply Filter";
            TimePickerGrid.Visibility = isScheduling ? Visibility.Visible : Visibility.Collapsed;           
            InitializeControls();
            DatePicker.SelectedDate = DateTime.Today;
        }

        private void InitializeControls()
        {
            // Initialize hour combo box (0-23)
            HourComboBox.ItemsSource = Enumerable.Range(0, 24).Select(h => h.ToString("D2"));
            HourComboBox.SelectedIndex = DateTime.Now.Hour;

            // Initialize minute combo box (0-59)
            MinuteComboBox.ItemsSource = Enumerable.Range(0, 60).Select(m => m.ToString("D2"));
            MinuteComboBox.SelectedIndex = DateTime.Now.Minute;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!DatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_isScheduling)
            {
                if (HourComboBox.SelectedItem == null || MinuteComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select hour and minute.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedHour = int.Parse(HourComboBox.SelectedItem.ToString()!);
                var selectedMinute = int.Parse(MinuteComboBox.SelectedItem.ToString()!);
                var selectedDate = DatePicker.SelectedDate.Value;

                SelectedDateTime = new DateTime(
                    selectedDate.Year,
                    selectedDate.Month,
                    selectedDate.Day,
                    selectedHour,
                    selectedMinute,
                    0
                );

                // If the selected time is in the past, schedule for next day
                if (SelectedDateTime < DateTime.Now)
                {
                    SelectedDateTime = SelectedDateTime.AddDays(1);
                }
            }
            else
            {
                // For filtering, use the selected date at midnight
                SelectedDateTime = DatePicker.SelectedDate.Value.Date;
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
