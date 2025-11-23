using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace clinicApp
{
    public partial class AddApointmentPage : Page
    {
        private readonly DataBaseManager dbManager = new DataBaseManager();

        public AddApointmentPage()
        {
            InitializeComponent();
        }

        // Load all patients into the ComboBox on page load
        private void PatientComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var patients = dbManager.GetAllPatients(); // Should return List<(string FirstName, string LastName)>
            PatientComboBox.ItemsSource = patients.Select(p => new PatientDisplay { FirstName = p.FirstName, LastName = p.LastName }).ToList();
            PatientComboBox.DisplayMemberPath = "FullName"; // Shows "First Last"
        }

        // When a patient is selected from the ComboBox, fill first and last name correctly
        private void PatientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientComboBox.SelectedItem is PatientDisplay selected)
            {
                // Set first name in the editable ComboBox box AFTER WPF sets FullName
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PatientComboBox.Text = selected.FirstName;
                }));

                // Last name goes to the read-only TextBox
                PatientLastName.Text = selected.LastName;
            }
        }


        public void SaveApointment_Click(object sender, RoutedEventArgs e)
        {
            int patientId = dbManager.GetPatientIdByName(PatientComboBox.Text, PatientLastName.Text);
            if (patientId == -1) return; // Patient not found, message already shown

            DateTime date = AppointmentDate.SelectedDate ?? DateTime.Now;
            TimeSpan time = TimeSpan.TryParse(AppointmentTime.Text, out var t) ? t : DateTime.Now.TimeOfDay;

            dbManager.AddAppointment(patientId, date, time, Description.Text);
            MessageBox.Show("Appointment added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Optional: clear form
            PatientComboBox.Text = "";
            PatientLastName.Clear();
            AppointmentTime.Clear();
            Description.Clear();
        }
    }

    // Helper class for ComboBox display
    public class PatientDisplay
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }
}
