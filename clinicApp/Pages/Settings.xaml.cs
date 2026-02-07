using System;
using System.Data.SQLite;
using System.IO;
using IOPath = System.IO.Path;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace clinicApp.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        private const string CredentialsDbFileName = "UserCredentials.db";
        private const string CredentialsTableName = "UserCredentials";
        private static readonly string[] AllowedLogoExtensions = [".svg", ".png", ".jpg", ".jpeg"];

        private string? _selectedLogoPath;
        private string? _currentLogoPath;
        private bool _isPasswordEnabled;

        public Settings()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCredentials();
            LoadPasswordState();
        }

        private void SaveCredentialsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCredentials();
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
            LogoFileNameText.Text = IOPath.GetFileName(_selectedLogoPath);
            LoadLogoPreview(_selectedLogoPath);
        }

        private void LoadLogoPreview(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                LogoPreviewImage.Source = null;
                return;
            }

            var ext = IOPath.GetExtension(path).ToLowerInvariant();
            if (ext == ".svg")
            {
                // SVG not directly supported in Image control, show placeholder text
                LogoPreviewImage.Source = null;
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                LogoPreviewImage.Source = bitmap;
            }
            catch
            {
                LogoPreviewImage.Source = null;
            }
        }

        private void LoadCredentials()
        {
            try
            {
                var dbPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialsDbFileName);
                if (!File.Exists(dbPath))
                    return;

                using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                conn.Open();

                using var cmd = new SQLiteCommand($"SELECT first_name, last_name, phone, email, ordre, specialty, clinic_name, clinic_address, logo_path FROM {CredentialsTableName} WHERE id = 1 LIMIT 1;", conn);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return;

                FirstNameTextBox.Text = reader["first_name"]?.ToString();
                LastNameTextBox.Text = reader["last_name"]?.ToString();
                PhoneTextBox.Text = reader["phone"]?.ToString();
                EmailTextBox.Text = reader["email"]?.ToString();
                OrdreTextBox.Text = reader["ordre"]?.ToString();
                SpecialtyTextBox.Text = reader["specialty"]?.ToString();
                ClinicNameTextBox.Text = reader["clinic_name"]?.ToString();
                ClinicAddressTextBox.Text = reader["clinic_address"]?.ToString();

                _currentLogoPath = reader["logo_path"]?.ToString();
                if (!string.IsNullOrEmpty(_currentLogoPath) && File.Exists(_currentLogoPath))
                {
                    LogoFileNameText.Text = IOPath.GetFileName(_currentLogoPath);
                    LoadLogoPreview(_currentLogoPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to load credentials", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveCredentials()
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
                    return;
                }

                string? storedLogoPath = _currentLogoPath;

                // Handle new logo selection
                if (!string.IsNullOrWhiteSpace(_selectedLogoPath))
                {
                    var ext = IOPath.GetExtension(_selectedLogoPath).ToLowerInvariant();
                    if (!AllowedLogoExtensions.Contains(ext))
                    {
                        MessageBox.Show("Invalid logo file type. Allowed: .svg, .png, .jpg, .jpeg",
                            "Invalid File",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    var assetsDir = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "User");
                    Directory.CreateDirectory(assetsDir);

                    storedLogoPath = IOPath.Combine(assetsDir, $"logo{ext}");
                    File.Copy(_selectedLogoPath, storedLogoPath, overwrite: true);
                }

                var dbPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialsDbFileName);
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

                // Add logo_path column if it doesn't exist (for existing databases)
                cmd.CommandText = $"PRAGMA table_info({CredentialsTableName});";
                var hasLogoColumn = false;
                using (var pragmaReader = cmd.ExecuteReader())
                {
                    while (pragmaReader.Read())
                    {
                        if (pragmaReader["name"]?.ToString() == "logo_path")
                        {
                            hasLogoColumn = true;
                            break;
                        }
                    }
                }

                if (!hasLogoColumn)
                {
                    cmd.CommandText = $"ALTER TABLE {CredentialsTableName} ADD COLUMN logo_path TEXT;";
                    cmd.ExecuteNonQuery();
                }

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
                cmd.Parameters.AddWithValue("@first_name", (object?)firstName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@last_name", (object?)lastName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone);
                cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
                cmd.Parameters.AddWithValue("@ordre", string.IsNullOrWhiteSpace(ordre) ? DBNull.Value : ordre);
                cmd.Parameters.AddWithValue("@specialty", string.IsNullOrWhiteSpace(specialty) ? DBNull.Value : specialty);
                cmd.Parameters.AddWithValue("@clinic_name", string.IsNullOrWhiteSpace(clinicName) ? DBNull.Value : clinicName);
                cmd.Parameters.AddWithValue("@clinic_address", string.IsNullOrWhiteSpace(clinicAddress) ? DBNull.Value : clinicAddress);
                cmd.Parameters.AddWithValue("@logo_path", string.IsNullOrWhiteSpace(storedLogoPath) ? DBNull.Value : storedLogoPath);

                cmd.ExecuteNonQuery();

                _currentLogoPath = storedLogoPath;
                _selectedLogoPath = null;

                MessageBox.Show("Credentials updated.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to save user info", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPasswordState()
        {
            _isPasswordEnabled = PasswordEntry.IsPasswordEnabled();
            PasswordToggle.IsChecked = _isPasswordEnabled;
            UpdatePasswordUI();
        }

        private void UpdatePasswordUI()
        {
            if (_isPasswordEnabled)
            {
                // Password is set - show fields for changing
                PasswordFieldsPanel.Visibility = Visibility.Visible;
                CurrentPasswordPanel.Visibility = Visibility.Visible;
                RemovePasswordButton.Visibility = Visibility.Visible;
                SetPasswordButton.Content = FindResource("BtnChangePassword");
            }
            else if (PasswordToggle.IsChecked == true)
            {
                // User wants to set a new password
                PasswordFieldsPanel.Visibility = Visibility.Visible;
                CurrentPasswordPanel.Visibility = Visibility.Collapsed;
                RemovePasswordButton.Visibility = Visibility.Collapsed;
                SetPasswordButton.Content = FindResource("BtnSetPassword");
            }
            else
            {
                // Password protection disabled
                PasswordFieldsPanel.Visibility = Visibility.Collapsed;
            }

            ClearPasswordFields();
        }

        private void ClearPasswordFields()
        {
            CurrentPasswordBox.Password = string.Empty;
            NewPasswordBox.Password = string.Empty;
            ConfirmPasswordBox.Password = string.Empty;
        }

        private void PasswordToggle_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordToggle.IsChecked == false && _isPasswordEnabled)
            {
                // User is trying to disable password - need to verify current password first
                var passwordDialog = new PasswordEntry();
                if (passwordDialog.ShowDialog() != true)
                {
                    // User didn't authenticate, revert toggle
                    PasswordToggle.IsChecked = true;
                    return;
                }
            }

            UpdatePasswordUI();
        }

        private void SetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var newPassword = NewPasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show(FindResource("PasswordRequired")?.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show(FindResource("PasswordMismatch")?.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_isPasswordEnabled)
            {
                // Changing password - verify current password
                var currentPassword = CurrentPasswordBox.Password;
                var currentHash = PasswordEntry.HashPassword(currentPassword);
                var storedHash = GetStoredPasswordHash();

                if (currentHash != storedHash)
                {
                    MessageBox.Show(FindResource("CurrentPasswordIncorrect")?.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Save the new password
            SavePassword(newPassword);
            _isPasswordEnabled = true;
            MessageBox.Show(
                _isPasswordEnabled ? FindResource("PasswordChangedSuccess")?.ToString() : FindResource("PasswordSetSuccess")?.ToString(),
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            UpdatePasswordUI();
        }

        private void RemovePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var currentPassword = CurrentPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                MessageBox.Show(FindResource("PasswordRequired")?.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var currentHash = PasswordEntry.HashPassword(currentPassword);
            var storedHash = GetStoredPasswordHash();

            if (currentHash != storedHash)
            {
                MessageBox.Show(FindResource("CurrentPasswordIncorrect")?.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Remove password
            SavePassword(null);
            _isPasswordEnabled = false;
            PasswordToggle.IsChecked = false;
            MessageBox.Show(FindResource("PasswordRemovedSuccess")?.ToString(), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdatePasswordUI();
        }

        private static string? GetStoredPasswordHash()
        {
            try
            {
                var dbPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialsDbFileName);
                if (!File.Exists(dbPath))
                    return null;

                using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                conn.Open();

                using var cmd = new SQLiteCommand($"SELECT password_hash FROM {CredentialsTableName} WHERE id = 1 LIMIT 1;", conn);
                var result = cmd.ExecuteScalar();
                return result?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static void SavePassword(string? password)
        {
            try
            {
                var dbPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialsDbFileName);
                var cs = $"Data Source={dbPath};Version=3;";

                using var conn = new SQLiteConnection(cs);
                conn.Open();

                using var cmd = new SQLiteCommand(conn);

                // Ensure table exists
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
    logo_path TEXT,
    password_hash TEXT
);";
                cmd.ExecuteNonQuery();

                // Check if password_hash column exists
                cmd.CommandText = $"PRAGMA table_info({CredentialsTableName});";
                var hasPasswordColumn = false;
                using (var pragmaReader = cmd.ExecuteReader())
                {
                    while (pragmaReader.Read())
                    {
                        if (pragmaReader["name"]?.ToString() == "password_hash")
                        {
                            hasPasswordColumn = true;
                            break;
                        }
                    }
                }

                if (!hasPasswordColumn)
                {
                    cmd.CommandText = $"ALTER TABLE {CredentialsTableName} ADD COLUMN password_hash TEXT;";
                    cmd.ExecuteNonQuery();
                }

                // Update password
                var passwordHash = password != null ? PasswordEntry.HashPassword(password) : null;
                cmd.CommandText = $"UPDATE {CredentialsTableName} SET password_hash = @password_hash WHERE id = 1;";
                cmd.Parameters.AddWithValue("@password_hash", (object?)passwordHash ?? DBNull.Value);

                var rowsAffected = cmd.ExecuteNonQuery();

                // If no rows updated, insert a new row
                if (rowsAffected == 0)
                {
                    cmd.CommandText = $"INSERT INTO {CredentialsTableName} (id, password_hash) VALUES (1, @password_hash);";
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to save password", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
