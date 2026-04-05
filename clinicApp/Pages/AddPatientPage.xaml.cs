using clinicApp.data;
using clinicApp.Services;
using System;
using System.Globalization;
using System.Threading;
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

            // Force dd/MM/yyyy format for the date picker input and display
            var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            culture.DateTimeFormat.DateSeparator = "/";
            Thread.CurrentThread.CurrentCulture = culture;

            DateOfBirth.Language = System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag);
            DateOfBirth.DateValidationError += (s, e) => e.ThrowException = false;
        }

        private void SavePatient_Click(object sender, RoutedEventArgs e)
        {
            string first = PatientFirstName.Text.Trim();
            string last = PatientLastName.Text.Trim();
            string phone = Phone.Text.Trim();
            string email = Email.Text.Trim();
            string note = Note.Text.Trim();

            DateTime? dateOfBirth = DateOfBirth.SelectedDate;
            string gender = (Gender.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Male";

            double? weight = null;
            if (double.TryParse(Weight.Text.Trim(), out double parsedWeight))
            {
                weight = parsedWeight;
            }

            string? bloodType = null;
            if (BloodAPos.IsChecked == true) bloodType = "A+";
            else if (BloodBPos.IsChecked == true) bloodType = "B+";
            else if (BloodABPos.IsChecked == true) bloodType = "AB+";
            else if (BloodOPos.IsChecked == true) bloodType = "O+";
            else if (BloodANeg.IsChecked == true) bloodType = "A-";
            else if (BloodBNeg.IsChecked == true) bloodType = "B-";
            else if (BloodABNeg.IsChecked == true) bloodType = "AB-";
            else if (BloodONeg.IsChecked == true) bloodType = "O-";

            // validation
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last))
            {
                MessageBox.Show("First and last name are required!", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Phone number is required.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dateOfBirth.HasValue)
            {
                MessageBox.Show("Date of birth is required.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                dbManager.AddPatient(first, last, phone, email, gender, dateOfBirth.Value, note, weight, bloodType);
                MessageBox.Show("Patient added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh the current page
                PageRefreshService.RefreshCurrentPage();

                // Close the QuickActionWindow and open AddMotiv
                int patientId = dbManager.GetPatientIdByName(first, last);
                var quickActionWindow = Window.GetWindow(this);
                var addMotivWindow = new Pages.AddMotiv(patientId, first, last);
                addMotivWindow.Show();
                quickActionWindow?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding patient:\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
