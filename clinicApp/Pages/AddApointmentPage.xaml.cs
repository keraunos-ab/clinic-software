using clinicApp.data;
using clinicApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace clinicApp
{
    public partial class AddApointmentPage : Page
    {
        private readonly DataBaseManager dbManager = new DataBaseManager();

        private List<PatientDisplay> _allPatients = new();

        public AddApointmentPage()
        {
            InitializeComponent();
            Loaded += AddApointmentPage_Loaded;
        }

        private void AddApointmentPage_Loaded(object sender, RoutedEventArgs e)
        {
            var patients = dbManager.GetAllPatients();
            _allPatients = patients
                .Select(p => new PatientDisplay { FirstName = p.FirstName, LastName = p.LastName })
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .ToList();
        }

        private void PatientSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = PatientSearchBox.Text.Trim();

            if (string.IsNullOrEmpty(query))
            {
                PatientPopup.IsOpen = false;
                PatientResultsList.ItemsSource = null;
                PatientLastName.Clear();
                return;
            }

            var results = _allPatients
                .Where(p =>
                    p.FirstName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    p.LastName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    p.FullName.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            PatientResultsList.ItemsSource = results;
            PatientPopup.IsOpen = results.Count > 0;
        }

        private void PatientResultsList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CommitSelectedPatient();
        }

        private void PatientSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!PatientPopup.IsOpen)
                return;

            if (e.Key == Key.Down)
            {
                PatientResultsList.SelectedIndex =
                    Math.Min(PatientResultsList.SelectedIndex + 1, PatientResultsList.Items.Count - 1);
                PatientResultsList.ScrollIntoView(PatientResultsList.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                PatientResultsList.SelectedIndex =
                    Math.Max(PatientResultsList.SelectedIndex - 1, 0);
                PatientResultsList.ScrollIntoView(PatientResultsList.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                CommitSelectedPatient();
                e.Handled = true;
            }
        }

        private void CommitSelectedPatient()
        {
            if (PatientResultsList.SelectedItem is not PatientDisplay selected)
                return;

            PatientSearchBox.Text = selected.FirstName;
            PatientLastName.Text = selected.LastName;

            PatientPopup.IsOpen = false;
            PatientSearchBox.Select(PatientSearchBox.Text.Length, 0);
        }

        public void SaveApointment_Click(object sender, RoutedEventArgs e)
        {
            int patientId = dbManager.GetPatientIdByName(PatientSearchBox.Text, PatientLastName.Text);
            if (patientId == -1) return;

            DateTime date = AppointmentDate.SelectedDate ?? DateTime.Now;
            TimeSpan time = TimeSpan.TryParse(AppointmentTime.Text, out var t) ? t : DateTime.Now.TimeOfDay;

            try
            {
                dbManager.AddAppointment(patientId, date, time, Description.Text);
                MessageBox.Show("Appointment added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                PatientSearchBox.Text = "";
                PatientLastName.Clear();
                AppointmentTime.Clear();
                Description.Clear();

                PatientPopup.IsOpen = false;
                PatientResultsList.ItemsSource = null;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Appointment Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
