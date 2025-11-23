using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static clinicApp.AddApointmentPage; // reuse PatientDisplay from appointment page

namespace clinicApp
{
    public partial class AddSessionPage : Page
    {
        private readonly DataBaseManager dbManager = new DataBaseManager();

        public AddSessionPage()
        {
            InitializeComponent();
        }

        private void PatientComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var patients = dbManager.GetAllPatients(); // List<(FirstName, LastName)>
            PatientComboBox.ItemsSource = patients.Select(p => new PatientDisplay
            {
                FirstName = p.FirstName,
                LastName = p.LastName
            }).ToList();

            PatientComboBox.DisplayMemberPath = "FullName";
        }

        private void PatientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientComboBox.SelectedItem is PatientDisplay selected)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PatientComboBox.Text = selected.FirstName;
                }));

                PatientLastName.Text = selected.LastName;
            }
        }

        private void SaveSession_Click(object sender, RoutedEventArgs e)
        {
            int patientId = dbManager.GetPatientIdByName(PatientComboBox.Text, PatientLastName.Text);
            if (patientId == -1)
            {
                MessageBox.Show("Patient not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DateTime date = SessionDate.SelectedDate ?? DateTime.Now;
            TimeSpan time = TimeSpan.TryParse(SessionTime.Text, out var t) ? t : DateTime.Now.TimeOfDay;

            dbManager.AddSession(patientId, date, time, Description.Text);

            MessageBox.Show("Session added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            PatientComboBox.Text = "";
            PatientLastName.Clear();
            SessionDate.SelectedDate = null;
            SessionTime.Clear();
            Description.Clear();
        }
    }
}
