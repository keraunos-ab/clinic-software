using System;
using System.Windows;
using System.Windows.Input;
using clinicApp.data;
using Npgsql;

namespace clinicApp.Pages
{
    /// <summary>
    /// Interaction logic for PasswordEntry.xaml
    /// </summary>
    public partial class PasswordEntry : Window
    {
        private const string CredentialsTableName = "UserCredentials";

        public bool IsAuthenticated { get; private set; }

        public PasswordEntry()
        {
            InitializeComponent();
            PasswordBox.Focus();
        }

        private void UnlockButton_Click(object sender, RoutedEventArgs e)
        {
            ValidatePassword();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ValidatePassword();
            }
        }

        private void ValidatePassword()
        {
            var enteredPassword = PasswordBox.Password;

            if (string.IsNullOrEmpty(enteredPassword))
            {
                ShowError();
                return;
            }

            var storedHash = GetStoredPasswordHash();
            if (string.IsNullOrEmpty(storedHash))
            {
                // No password set, allow access
                IsAuthenticated = true;
                Close();
                return;
            }

            var enteredHash = HashPassword(enteredPassword);
            if (enteredHash == storedHash)
            {
                IsAuthenticated = true;
                Close();
            }
            else
            {
                ShowError();
                PasswordBox.Password = string.Empty;
                PasswordBox.Focus();
            }
        }

        private void ShowError()
        {
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private static string? GetStoredPasswordHash()
        {
            try
            {
                using var conn = new NpgsqlConnection(DataBaseManager.DefaultConnectionString);
                conn.Open();

                // Check if UserCredentials table exists
                using var tableCheckCmd = new NpgsqlCommand(
                    "SELECT 1 FROM information_schema.tables WHERE table_name = 'usercredentials' LIMIT 1", conn);
                if (tableCheckCmd.ExecuteScalar() is null)
                    return null;

                // Check if password_hash column exists
                using var colCheckCmd = new NpgsqlCommand(
                    "SELECT 1 FROM information_schema.columns WHERE table_name = 'usercredentials' AND column_name = 'password_hash' LIMIT 1", conn);
                if (colCheckCmd.ExecuteScalar() is null)
                    return null;

                using var cmd = new NpgsqlCommand($"SELECT password_hash FROM {CredentialsTableName} WHERE id = 1 LIMIT 1", conn);
                var result = cmd.ExecuteScalar();
                return result?.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static string HashPassword(string password)
        {
            return CryptoHelper.HashPassword(password);
        }

        public static bool IsPasswordEnabled()
        {
            var hash = GetStoredPasswordHash();
            return !string.IsNullOrEmpty(hash);
        }
    }
}
