using System;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace clinicApp.Pages
{
    /// <summary>
    /// Interaction logic for PasswordEntry.xaml
    /// </summary>
    public partial class PasswordEntry : Window
    {
        private const string CredentialsDbFileName = "UserCredentials.db";
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
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialsDbFileName);
                if (!File.Exists(dbPath))
                    return null;

                using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                conn.Open();

                // Check if password_hash column exists
                using var pragmaCmd = new SQLiteCommand($"PRAGMA table_info({CredentialsTableName});", conn);
                var hasPasswordColumn = false;
                using (var pragmaReader = pragmaCmd.ExecuteReader())
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
                    return null;

                using var cmd = new SQLiteCommand($"SELECT password_hash FROM {CredentialsTableName} WHERE id = 1 LIMIT 1;", conn);
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
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public static bool IsPasswordEnabled()
        {
            var hash = GetStoredPasswordHash();
            return !string.IsNullOrEmpty(hash);
        }
    }
}
