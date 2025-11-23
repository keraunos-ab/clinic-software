using clinicApp.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace clinicApp
{
    public partial class AppointmentsPage : Page
    {
        private readonly DataBaseManager _db = new();

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

                // Join appointments with patient names
                var joined = from a in appointments
                             join p in patients on a.PatientId equals p.Id
                             select new
                             {
                                 PatientName = $"{p.FirstName} {p.LastName}",
                                 Date = a.Date.ToString("yyyy-MM-dd"),
                                 Time = a.Time.ToString("HH:mm"),
                                 Note = string.IsNullOrEmpty(a.Note) ? "—" : a.Note
                             };

                // Sort by date ascending, then time ascending
                var ordered = joined
                    .OrderBy(a => DateOnly.Parse(a.Date))
                    .ThenBy(a => TimeOnly.Parse(a.Time))
                    .ToList();

                AppointmentsList.ItemsSource = ordered;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointments: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AppointmentsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
