using clinicApp.data;
using System;
using System.Data.SQLite;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;

namespace clinicApp
{
    public partial class HomePage : Page
    {
        private const string CredentialsDbFileName = "UserCredentials.db";
        private const string CredentialsTableName = "UserCredentials";

        private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };

        public HomePage()
        {
            InitializeComponent();

            DoctorNameText.Text = $"Dr {GetDoctorNameFromCredentialsDb()}";

            UpdateDateTimeText();
            _clockTimer.Tick += (_, __) => UpdateDateTimeText();
            _clockTimer.Start();

            var db = new DataBaseManager();
            var count = db.GetTodaysAppointmentsCount();
            TodaysCountText.Text = count.ToString();
        }

        private void UpdateDateTimeText()
        {
            // Example: "Thu, Jan 23  •  14:05:09"
            DateTimeText.Text = DateTime.Now.ToString("ddd, MMM d  •  HH:mm:ss");
        }

        private static string GetDoctorNameFromCredentialsDb()
        {
            try
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialsDbFileName);
                if (!File.Exists(dbPath))
                    return "—";

                using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                conn.Open();

                using var cmd = new SQLiteCommand(
                    $"SELECT first_name, last_name FROM {CredentialsTableName} WHERE id = 1 LIMIT 1;",
                    conn);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return "—";

                var first = reader["first_name"]?.ToString()?.Trim();
                var last = reader["last_name"]?.ToString()?.Trim();

                var full = $"{first} {last}".Trim();
                return string.IsNullOrWhiteSpace(full) ? "—" : full;
            }
            catch
            {
                return "—";
            }
        }
    }
}