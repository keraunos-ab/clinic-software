using System.Collections.Generic;
using System.Linq;
using System.Windows;
using clinicApp.Models;

namespace clinicApp
{
    public partial class PatientHistoryWindow : Window
    {
        public PatientHistoryWindow(string firstName, string lastName, List<Checkup> checkups, string? title = null)
        {
            InitializeComponent();

            PatientNameTextBlock.Text = $"{firstName} {lastName}";

            if (title != null)
                HeaderTitleText.Text = title;

            // Sort checkups descending by date/time
            var sortedCheckups = checkups
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.Time)
                .ToList();

            // Bind checkups to ItemsControl
            CheckupsList.ItemsSource = sortedCheckups;
        }
    }
}
