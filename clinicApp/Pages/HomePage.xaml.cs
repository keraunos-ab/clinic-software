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

        private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };

        public HomePage()
        {
            InitializeComponent();
            var db = new DataBaseManager();

            UpdateDateTimeText();
            _clockTimer.Tick += (_, __) => UpdateDateTimeText();
            _clockTimer.Start();

            WelcomeDr.Text = "Welcome back\nDr " + db.GetDoctorCredentials().getFirstName() + " " + Char.ToUpper(db.GetDoctorCredentials().getLastName()[0]);
            AppointmentCounter.Text = "You have " + db.GetTodaysAppointmentsCount() + " appointment(s) today.";
            SessionCouter.Text = "You did " + db.GetTodaysSessionCount() + " session(s) today.";
        }

        private void UpdateDateTimeText()
        {
            // Example: "Thu, Jan 23  •  14:05:09"
            DateTimeText.Text = DateTime.Now.ToString("ddd, MMM d  •  HH:mm:ss");
        }
    }
}