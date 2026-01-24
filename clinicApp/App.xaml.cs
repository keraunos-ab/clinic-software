using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using clinicApp.Pages;

namespace clinicApp
{
    public partial class App : Application
    {
        private const string CredentialsDbFileName = "UserCredentials.db";
        private const string CredentialsTableName = "UserCredentials";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            if (IsFirstRun())
            {
                MainWindow = new IntroductionWindow();
                MainWindow.Show();
                return;
            }

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
