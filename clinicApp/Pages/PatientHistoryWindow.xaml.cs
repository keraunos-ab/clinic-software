using System.Collections.Generic;
using System.Linq;
using System.Windows;
using clinicApp.Models;

namespace clinicApp
{
    public partial class PatientHistoryWindow : Window
    {
        public PatientHistoryWindow(string firstName, string lastName, List<Session> sessions)
        {
            InitializeComponent();

            // Show patient's full name
            PatientNameTextBlock.Text = $"{firstName} {lastName}";

            // Sort sessions descending by date/time
            var sortedSessions = sessions
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.Time)
                .ToList();

            // Bind sessions to ItemsControl
            SessionsList.ItemsSource = sortedSessions;
        }
    }
}
