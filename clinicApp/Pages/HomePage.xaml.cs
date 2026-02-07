using clinicApp.data;
using clinicApp.Services;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace clinicApp
{
    public partial class HomePage : Page
    {
        private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly DataBaseManager _db;

        public HomePage()
        {
            InitializeComponent();
            _db = new DataBaseManager();

            UpdateDateTimeText();
            _clockTimer.Tick += (_, __) => UpdateDateTimeText();
            _clockTimer.Start();

            UpdateLocalizedTexts();

            // Subscribe to language changes
            LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            UpdateLocalizedTexts();
        }

        private void UpdateLocalizedTexts()
        {
            var credentials = _db.GetDoctorCredentials();
            string firstName = credentials.getFirstName();
            string lastName = credentials.getLastName();
            string lastInitial = !string.IsNullOrEmpty(lastName) ? char.ToUpper(lastName[0]).ToString() : "";

            // Get localized strings from resources
            string welcomeBack = TryFindResource("WelcomeBack") as string ?? "Welcome back";
            string drPrefix = TryFindResource("DrPrefix") as string ?? "Dr";
            string appointmentFormat = TryFindResource("AppointmentCount") as string ?? "You have {0} appointment(s) today.";
            string sessionFormat = TryFindResource("SessionCount") as string ?? "You did {0} session(s) today.";

            // Update welcome text
            WelcomeDr.Text = $"{welcomeBack}\n{drPrefix} {firstName} {lastInitial}";

            // Update counters
            int appointmentCount = _db.GetTodaysAppointmentsCount();
            int sessionCount = _db.GetTodaysSessionCount();
            AppointmentCounter.Text = string.Format(appointmentFormat, appointmentCount);
            SessionCouter.Text = string.Format(sessionFormat, sessionCount);

            // Update FlowDirection for RTL languages
            FlowDirection = LanguageManager.Instance.GetFlowDirection();
        }

        private void UpdateDateTimeText()
        {
            // Get the date format from resources
            string dateFormat = TryFindResource("DateTimeFormat") as string ?? "ddd, MMM d  •  HH:mm:ss";
            
            // Get the appropriate culture based on current language
            CultureInfo culture = GetCurrentCulture();
            
            DateTimeText.Text = DateTime.Now.ToString(dateFormat, culture);
        }

        private static CultureInfo GetCurrentCulture()
        {
            return LanguageManager.Instance.CurrentLanguage switch
            {
                "French" => new CultureInfo("fr-FR"),
                "Arabic" => new CultureInfo("ar-SA"),
                _ => new CultureInfo("en-US")
            };
        }
    }
}