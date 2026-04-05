using clinicApp.data;
using clinicApp.Models;
using clinicApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace clinicApp
{
    public partial class AddSessionPage : Page
    {
        private readonly DataBaseManager dbManager = new DataBaseManager();
        private List<PatientDisplay> _allPatients = new();
        private int _selectedPatientId = -1;
        private List<ConsultationItem> _consultationItems = new();

        public AddSessionPage()
        {
            InitializeComponent();
            Loaded += AddSessionPage_Loaded;
        }

        private void AddSessionPage_Loaded(object sender, RoutedEventArgs e)
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
                ClearConsultationDropdown();
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

            _selectedPatientId = dbManager.GetPatientIdByName(selected.FirstName, selected.LastName);
            LoadConsultationsForPatient();
        }

        private void LoadConsultationsForPatient()
        {
            _consultationItems.Clear();
            ConsultationComboBox.ItemsSource = null;
            ConsultationComboBox.SelectedIndex = -1;
            SaveButton.IsEnabled = false;

            if (_selectedPatientId == -1) return;

            var consultations = dbManager.GetConsultationsByPatient(_selectedPatientId);
            _consultationItems = consultations
                .Select(c => new ConsultationItem
                {
                    Id = c.Id,
                    DisplayText = $"{c.Date}{(string.IsNullOrEmpty(c.Motiv) ? "" : $" — {c.Motiv}")}"
                })
                .ToList();

            ConsultationComboBox.ItemsSource = _consultationItems;
        }

        private void ClearConsultationDropdown()
        {
            _selectedPatientId = -1;
            _consultationItems.Clear();
            ConsultationComboBox.ItemsSource = null;
            ConsultationComboBox.SelectedIndex = -1;
            SaveButton.IsEnabled = false;
        }

        private void ConsultationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveButton.IsEnabled = ConsultationComboBox.SelectedItem is ConsultationItem;
        }

        private void SaveSession_Click(object sender, RoutedEventArgs e)
        {
            if (ConsultationComboBox.SelectedItem is not ConsultationItem selectedConsultation)
            {
                MessageBox.Show("Please select a consultation.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int patientId = dbManager.GetPatientIdByName(PatientSearchBox.Text, PatientLastName.Text);
            if (patientId == -1)
            {
                MessageBox.Show("Patient not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DateTime date = SessionDate.SelectedDate ?? DateTime.Now;
            TimeSpan time = TimeSpan.TryParse(SessionTime.Text, out var t) ? t : DateTime.Now.TimeOfDay;

            dbManager.AddSession(patientId, selectedConsultation.Id, date, time, Description.Text);

            MessageBox.Show("Checkup added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            PatientSearchBox.Text = "";
            PatientLastName.Clear();
            SessionDate.SelectedDate = null;
            SessionTime.Clear();
            Description.Clear();
            ClearConsultationDropdown();

            PatientPopup.IsOpen = false;
            PatientResultsList.ItemsSource = null;

            // Refresh the current page
            PageRefreshService.RefreshCurrentPage();
        }

        private class ConsultationItem
        {
            public int Id { get; set; }
            public string DisplayText { get; set; } = "";
        }
    }
}
