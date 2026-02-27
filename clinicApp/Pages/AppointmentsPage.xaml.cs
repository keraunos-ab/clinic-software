using clinicApp.data;
using clinicApp.Services;
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

        private const int ExpiredThresholdHours = 1;

        public AppointmentsPage()
        {
            InitializeComponent();
            
            // Apply RTL for Arabic language
            FlowDirection = LanguageManager.Instance.GetFlowDirection();
            LanguageManager.Instance.LanguageChanged += (_, _) => 
                FlowDirection = LanguageManager.Instance.GetFlowDirection();
            
            LoadAppointments();
        }

        private void OnAppointmentsChanged(object? sender, EventArgs e)
        {
            LoadAppointments();
        }

        private void AppointmentsList_SizeChanged(object sender, RoutedEventArgs e)
        {
            double width = AppointmentsList.ActualWidth;
            if (width <=0 )
            {
                PatientColomn.Width = width * 0.25;
                DateColomn.Width = width * 0.20;
                TimeColomn.Width = width * 0.15;
                NoteColomn.Width = width * 0.25;
                DoneColomn.Width = width * 0.10;
            }
            else
            {
                PatientColomn.Width = width * 0.25;
                DateColomn.Width = width * 0.20;
                TimeColomn.Width = width * 0.15;
                NoteColomn.Width = width * 0.25;
                DoneColomn.Width = width * 0.10;
            }
        }

        private void LoadAppointments()
        {
            try
            {
                var appointments = _db.GetAllAppointments();
                var patients = _db.GetAllPatients();
                var now = DateTime.Now;
                var expiredCutoff = now.AddHours(-ExpiredThresholdHours);

                var allAppointments =
                    (from a in appointments
                     join p in patients on a.PatientId equals p.Id
                     select new AppointmentRow(
                         a.Id,
                         $"{p.FirstName} {p.LastName}".Trim(),
                         a.Date.ToString("yyyy-MM-dd"),
                         a.Time.ToString("HH:mm"),
                         string.IsNullOrWhiteSpace(a.Note) ? "—" : a.Note,
                         ToLocalDateTime(a.Date, a.Time)))
                    .ToList();

                var activeAppointments = allAppointments
                    .Where(x => x.When >= expiredCutoff)
                    .OrderBy(x => x.When)
                    .ToList();

                var expiredAppointments = allAppointments
                    .Where(x => x.When < expiredCutoff)
                    .OrderByDescending(x => x.When)
                    .ToList();

                _all = activeAppointments.Concat(expiredAppointments).ToList();

                AppointmentsList.ItemsSource = _all;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointments: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not AppointmentRow row)
            {
                return;
            }

            try
            {
                _db.DeleteAppointment(row.Id);
                _all.Remove(row);

                AppointmentsList.ItemsSource = null;
                AppointmentsList.ItemsSource = _all;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting appointment: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static DateTime ToLocalDateTime(DateOnly date, TimeOnly time)
        {
            return date.ToDateTime(time);
        }

        private sealed record AppointmentRow(int Id, string PatientName, string Date, string Time, string Note, DateTime When);
    }
}
