using System;
using System.Windows;
using clinicApp.data;
using clinicApp.Pages;
using clinicApp.Services;
using Npgsql;

namespace clinicApp
{
    public partial class App : Application
    {
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
                using var conn = new NpgsqlConnection(DataBaseManager.DefaultConnectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand(
                    "SELECT 1 FROM information_schema.tables WHERE table_name = 'usercredentials' LIMIT 1",
                    conn);

                if (cmd.ExecuteScalar() is null)
                    return true;

                using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM {CredentialsTableName}", conn);
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
