using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace clinicApp.Pages
{
    public partial class Introduction : Page
    {
        private const string CredentialsDbFileName = "UserCredentials.db";
        private const string CredentialsTableName = "UserCredentials";

        public Introduction()
        {
            InitializeComponent();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySaveCredentials())
                return;

            if (Window.GetWindow(this) is IntroductionWindow introWindow)
            {
                introWindow.FinishAndOpenMain();
                return;
            }

            // Fallback (shouldn't happen if hosted in IntroductionWindow)
            Application.Current.MainWindow = new clinicApp.MainWindow();
            Application.Current.MainWindow.Show();
            Window.GetWindow(this)?.Close();
        }

        private bool TrySaveCredentials()
        {
            try
            {
                var firstName = FirstNameTextBox.Text?.Trim();
                var lastName = LastNameTextBox.Text?.Trim();
                var phone = PhoneTextBox.Text?.Trim();
                var email = EmailTextBox.Text?.Trim();
                var ordre = OrdreTextBox.Text?.Trim();
                var specialty = SpecialtyTextBox.Text?.Trim();
                var clinicName = ClinicNameTextBox.Text?.Trim();
                var clinicAddress = ClinicAddressTextBox.Text?.Trim();

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    MessageBox.Show("First Name and Last Name are required.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialsDbFileName);
                var cs = $"Data Source={dbPath};Version=3;";

                using var conn = new SQLiteConnection(cs);
                conn.Open();

                using var cmd = new SQLiteCommand(conn);

                cmd.CommandText = $@"
CREATE TABLE IF NOT EXISTS {CredentialsTableName} (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    first_name TEXT,
    last_name TEXT,
    phone TEXT,
    email TEXT,
    ordre TEXT,
    specialty TEXT,
    clinic_name TEXT,
    clinic_address TEXT
);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = $@"
INSERT INTO {CredentialsTableName}
(id, first_name, last_name, phone, email, ordre, specialty, clinic_name, clinic_address)
VALUES
(1, @first_name, @last_name, @phone, @email, @ordre, @specialty, @clinic_name, @clinic_address)
ON CONFLICT(id) DO UPDATE SET
    first_name = excluded.first_name,
    last_name = excluded.last_name,
    phone = excluded.phone,
    email = excluded.email,
    ordre = excluded.ordre,
    specialty = excluded.specialty,
    clinic_name = excluded.clinic_name,
    clinic_address = excluded.clinic_address;";
                cmd.Parameters.AddWithValue("@first_name", (object?)firstName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@last_name", (object?)lastName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone);
                cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
                cmd.Parameters.AddWithValue("@ordre", string.IsNullOrWhiteSpace(ordre) ? DBNull.Value : ordre);
                cmd.Parameters.AddWithValue("@specialty", string.IsNullOrWhiteSpace(specialty) ? DBNull.Value : specialty);
                cmd.Parameters.AddWithValue("@clinic_name", string.IsNullOrWhiteSpace(clinicName) ? DBNull.Value : clinicName);
                cmd.Parameters.AddWithValue("@clinic_address", string.IsNullOrWhiteSpace(clinicAddress) ? DBNull.Value : clinicAddress);

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to save user info", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
