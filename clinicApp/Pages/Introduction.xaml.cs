using clinicApp.data;
using clinicApp.Services;
using Microsoft.Win32;
using Npgsql;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace clinicApp.Pages
{
    public partial class Introduction : Page
    {
        private const string CredentialsTableName = "UserCredentials";

        private static readonly string[] AllowedLogoExtensions = [".svg", ".png", ".jpg", ".jpeg"];

        private string? _selectedLogoPath;

        public Introduction()
        {
            InitializeComponent();
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            FlowDirection = LanguageManager.Instance.GetFlowDirection();
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageSelector?.SelectedIndex == null || LanguageSelector.SelectedIndex < 0) return;

            LanguageManager.Instance.SetLanguageByIndex(LanguageSelector.SelectedIndex);
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

                // Initialize database (creates PostgreSQL database and tables)
                var dbManager = new DataBaseManager();
                dbManager.InitializeDatabase();

                using var conn = new NpgsqlConnection(DataBaseManager.DefaultConnectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand($@"
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
    logo_path = excluded.logo_path;", conn);
                cmd.Parameters.AddWithValue("@first_name", CryptoHelper.Encrypt(firstName));
                cmd.Parameters.AddWithValue("@last_name", CryptoHelper.Encrypt(lastName));
                cmd.Parameters.AddWithValue("@phone", CryptoHelper.Encrypt(phone));
                cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : (object)CryptoHelper.Encrypt(email));
                cmd.Parameters.AddWithValue("@ordre", string.IsNullOrWhiteSpace(ordre) ? DBNull.Value : (object)CryptoHelper.Encrypt(ordre));
                cmd.Parameters.AddWithValue("@specialty", CryptoHelper.Encrypt(specialty));
                cmd.Parameters.AddWithValue("@clinic_name", CryptoHelper.Encrypt(clinicName));
                cmd.Parameters.AddWithValue("@clinic_address", CryptoHelper.Encrypt(clinicAddress));
                cmd.Parameters.AddWithValue("@logo_path", string.IsNullOrWhiteSpace(storedLogoPath) ? DBNull.Value : (object)CryptoHelper.Encrypt(storedLogoPath));

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