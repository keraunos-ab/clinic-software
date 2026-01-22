using clinicApp.data;
using System.Windows;
using System.Windows.Controls;

namespace clinicApp
{
    public partial class AddPatientPage : Page
    {
        private readonly DataBaseManager dbManager = new DataBaseManager();

        public AddPatientPage()
        {
            InitializeComponent();
        }

        private void SavePatient_Click(object sender, RoutedEventArgs e)
        {
            string first = PatientFirstName.Text.Trim();
            string last = PatientLastName.Text.Trim();
            string phone = Phone.Text.Trim();
            string email = Email.Text.Trim();
            string note = Note.Text.Trim();

            // validation
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last))
            {
                MessageBox.Show("First and last name are required!", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Please provide at least a phone number or an email.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                dbManager.AddPatient(first, last, phone, email, note);
                MessageBox.Show("Patient added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // optional: clear form
                PatientFirstName.Clear();
                PatientLastName.Clear();
                Phone.Clear();
                Email.Clear();
                Note.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding patient:\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
