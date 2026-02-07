using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using clinicApp.Pages;
using clinicApp.Services;

namespace clinicApp
{
    public partial class App : Application
    {
        private const string CredentialsDbFileName = "UserCredentials.db";
        private const string CredentialsTableName = "UserCredentials";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Prevent auto-shutdown before any dialogs
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            // Apply saved language BEFORE creating any windows
            LanguageManager.Instance.ApplyLanguage();

            if (IsFirstRun())
            {
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                MainWindow = new IntroductionWindow();
                MainWindow.Show();
                return;
            }

            // Check if password protection is enabled
            if (PasswordEntry.IsPasswordEnabled())
            {
                var passwordDialog = new PasswordEntry();
                passwordDialog.ShowDialog();

                if (!passwordDialog.IsAuthenticated)
                {
                    // User closed the dialog without authenticating or failed
                    Shutdown();
                    return;
                }
            }

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        private static bool IsFirstRun()
        {
            try
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialsDbFileName);
                if (!File.Exists(dbPath))
                    return true;

                using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                conn.Open();

                using var cmd = new SQLiteCommand(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name=@t LIMIT 1;",
                    conn);
                cmd.Parameters.AddWithValue("@t", CredentialsTableName);

                if (cmd.ExecuteScalar() is null)
                    return true;

                using var countCmd = new SQLiteCommand($"SELECT COUNT(*) FROM {CredentialsTableName};", conn);
                var count = Convert.ToInt64(countCmd.ExecuteScalar());

                return count == 0;
            }
            catch
            {
                return true;
            }
        }
    }
}
