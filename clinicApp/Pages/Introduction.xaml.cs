using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace clinicApp.Pages
{
    public partial class Introduction : Page
    {
        private const string CredentialsDbFileName = "UserCredentials.db";
        private const string CredentialsTableName = "UserCredentials";

        private static readonly string[] AllowedLogoExtensions = [".svg", ".png", ".jpg", ".jpeg"];

        private string? _selectedLogoPath;

        public Introduction()
        {
            InitializeComponent();
        }

        private void BrowseLogoButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select a logo/image",
                Filter = "Images (*.svg;*.png;*.jpg;*.jpeg)|*.svg;*.png;*.jpg;*.jpeg",
                CheckFileExists = true
            };

            if (dlg.ShowDialog() != true)
                return;

            _selectedLogoPath = dlg.FileName;
            LogoBrowseButton.Content = Path.GetFileName(_selectedLogoPath);
        }

        // Window buttons (hosted inside IntroductionWindow)
        private static Window? GetHostWindow(DependencyObject d) => Window.GetWindow(d);

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            var w = GetHostWindow(this);
            if (w is null) return;
            w.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            var w = GetHostWindow(this);
            if (w is null) return;
            w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            GetHostWindow(this)?.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var w = GetHostWindow(this);
            if (w is null) return;

            if (e.ClickCount == 2)
            {
                w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                return;
            }

            w.DragMove();
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
                var email = EmailTextBox.Text?.Trim(); // optional
                var ordre = OrdreTextBox.Text?.Trim(); // optional
                var specialty = SpecialtyTextBox.Text?.Trim();
                var clinicName = ClinicNameTextBox.Text?.Trim();
                var clinicAddress = ClinicAddressTextBox.Text?.Trim();

                // Required: First/Last/Phone/Specialty/ClinicName/ClinicAddress
                if (string.IsNullOrWhiteSpace(firstName) ||
                    string.IsNullOrWhiteSpace(lastName) ||
                    string.IsNullOrWhiteSpace(phone) ||
                    string.IsNullOrWhiteSpace(specialty) ||
                    string.IsNullOrWhiteSpace(clinicName) ||
                    string.IsNullOrWhiteSpace(clinicAddress))
                {
                    MessageBox.Show(
                        "Required fields: First Name, Last Name, Phone Number, Doctor Specialty, Clinic Name, Clinic Address.\n" +
                        "Optional fields: Email, N° ordre, Logo/Image.",
                        "Missing Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }

                string? storedLogoPath = null;

                if (!string.IsNullOrWhiteSpace(_selectedLogoPath))
                {
                    var ext = Path.GetExtension(_selectedLogoPath).ToLowerInvariant();
                    if (!AllowedLogoExtensions.Contains(ext))
                    {
                        MessageBox.Show("Invalid logo file type. Allowed: .svg, .png, .jpg, .jpeg",
                            "Invalid File",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return false;
                    }

                    var assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "User");
                    Directory.CreateDirectory(assetsDir);

                    storedLogoPath = Path.Combine(assetsDir, $"logo{ext}");
                    File.Copy(_selectedLogoPath, storedLogoPath, overwrite: true);
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
    clinic_address TEXT,
    logo_path TEXT
);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = $@"
INSERT INTO {CredentialsTableName}
(id, first_name, last_name, phone, email, ordre, specialty, clinic_name, clinic_address, logo_path)
VALUES
(1, @first_name, @last_name, @phone, @email, @ordre, @specialty, @clinic_name, @clinic_address, @logo_path)
ON CONFLICT(id) DO UPDATE SET
    first_name = excluded.first_name,
    last_name = excluded.last_name,
    phone = excluded.phone,
    email = excluded.email,
    ordre = excluded.ordre,
    specialty = excluded.specialty,
    clinic_name = excluded.clinic_name,
    clinic_address = excluded.clinic_address,
    logo_path = excluded.logo_path;";
                cmd.Parameters.AddWithValue("@first_name", firstName);
                cmd.Parameters.AddWithValue("@last_name", lastName);
                cmd.Parameters.AddWithValue("@phone", phone);
                cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
                cmd.Parameters.AddWithValue("@ordre", string.IsNullOrWhiteSpace(ordre) ? DBNull.Value : ordre);
                cmd.Parameters.AddWithValue("@specialty", specialty);
                cmd.Parameters.AddWithValue("@clinic_name", clinicName);
                cmd.Parameters.AddWithValue("@clinic_address", clinicAddress);
                cmd.Parameters.AddWithValue("@logo_path", string.IsNullOrWhiteSpace(storedLogoPath) ? DBNull.Value : storedLogoPath);

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