using System;
using System.Linq;
using System.Windows;

namespace BatchMonitor.Views
{
    public partial class ChangeScheduleDialog : Window
    {
        public DateTime SelectedDateTime { get; private set; }

        public ChangeScheduleDialog()
        {
            InitializeComponent();
            InitializeControls();
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
            if (HourComboBox.SelectedItem == null || MinuteComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select hour and minute.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedHour = int.Parse(HourComboBox.SelectedItem.ToString()!);
            var selectedMinute = int.Parse(MinuteComboBox.SelectedItem.ToString()!);

            // Use today's date with selected time, but if the time has passed, use tomorrow
            var today = DateTime.Today;
            var selectedTime = new DateTime(today.Year, today.Month, today.Day, selectedHour, selectedMinute, 0);

            // If the selected time is in the past today, schedule for tomorrow
            if (selectedTime < DateTime.Now)
            {
                selectedTime = selectedTime.AddDays(1);
            }

            SelectedDateTime = selectedTime;

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