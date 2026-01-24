using clinicApp.data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace clinicApp
{
    public partial class AppointmentsPage : Page
    {
        private readonly DataBaseManager _db = new();
        private List<AppointmentRow> _all = new();

        public AppointmentsPage()
        {
            InitializeComponent();
            LoadAppointments();
        }

        private void LoadAppointments()
        {
            try
            {
                var appointments = _db.GetAllAppointments();
                var patients = _db.GetAllPatients();

                _all =
                    (from a in appointments
                     join p in patients on a.PatientId equals p.Id
                     select new AppointmentRow(
                         a.Id,
                         $"{p.FirstName} {p.LastName}".Trim(),
                         a.Date.ToString("yyyy-MM-dd"),
                         a.Time.ToString("HH:mm"),
                         string.IsNullOrWhiteSpace(a.Note) ? "—" : a.Note,
                         ToLocalDateTime(a.Date, a.Time)))
                    // Closest upcoming first (past ones will naturally sink to bottom)
                    .OrderBy(x => x.When)
                    .ToList();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointments: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static DateTime ToLocalDateTime(DateOnly date, TimeOnly time)
        {
            // Unspecified kind is fine here; it's local "wall clock" ordering.
            return date.ToDateTime(time);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            var q = SearchTextBox.Text?.Trim() ?? string.Empty;

            IEnumerable<AppointmentRow> query = _all;

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLower = q.ToLowerInvariant();
                query = query.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.PatientName) && x.PatientName.ToLowerInvariant().Contains(qLower)) ||
                    (!string.IsNullOrWhiteSpace(x.Note) && x.Note.ToLowerInvariant().Contains(qLower)) ||
                    (!string.IsNullOrWhiteSpace(x.Date) && x.Date.Contains(qLower, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(x.Time) && x.Time.Contains(qLower, StringComparison.OrdinalIgnoreCase)));
            }

            // Always keep nearest-first ordering after filtering.
            AppointmentsList.ItemsSource = query.OrderBy(x => x.When).ToList();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            if (btn.DataContext is not AppointmentRow row)
                return;

            var result = MessageBox.Show(
                $"Mark appointment #{row.Id} as done?\n\n{row.PatientName} • {row.Date} {row.Time}",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _db.DeleteAppointment(row.Id);

                // Remove from in-memory list and refresh view.
                _all.RemoveAll(x => x.Id == row.Id);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete appointment: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private sealed record AppointmentRow(int Id, string PatientName, string Date, string Time, string Note, DateTime When);
    }
}
