using clinicApp.data;
using clinicApp.Pages;
using clinicApp.Services;
using System;
using System.Collections.Generic;
using Npgsql;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace clinicApp
{
    public partial class MainWindow : Window
    {
        private const string CredentialsTableName = "UserCredentials";

        private readonly DispatcherTimer _topBarClockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private bool _isInitializing = true;

        private readonly List<System.Type> _pageCycleOrder =
        [
            typeof(HomePage),
            typeof(PatientsPage),
            typeof(AppointmentsPage),
            typeof(PrescriptionPage),
            typeof(Settings),
        ];

        public MainWindow()
        {
            InitializeComponent();

            DataBaseManager db = new DataBaseManager();
            db.InitializeDatabase();

            MainFrame.Navigated += MainFrame_Navigated;

            // Global hotkeys for this window
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            UpdateTopBarDateTime();
            _topBarClockTimer.Tick += (_, __) => UpdateTopBarDateTime();
            _topBarClockTimer.Start();

            // Set up language manager
            LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
            LanguageSelector.SelectedIndex = LanguageManager.Instance.GetCurrentLanguageIndex();
            FlowDirection = LanguageManager.Instance.GetFlowDirection();

            // Set up theme manager - apply saved theme and sync toggle
            ThemeManager.Instance.ApplyTheme();
            ThemeSwitch.IsChecked = ThemeManager.Instance.IsDarkTheme;
            _isInitializing = false;

            if (IsFirstRun())
            {
                MainFrame.Navigate(new Introduction());
                SetActiveNav(NavHomeButton);
            }
            else
            {
                MainFrame.Navigate(new HomePage());
                SetActiveNav(NavHomeButton);
            }
            TitleText.Text = "☆ Welcome to " + db.GetDoctorCredentials().getClinicName() + " Software";
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            FlowDirection = LanguageManager.Instance.GetFlowDirection();
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

                var tableExists = cmd.ExecuteScalar() is not null;
                if (!tableExists)
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

        private void UpdateTopBarDateTime()
        {
            // Get the appropriate culture based on current language
            CultureInfo culture = LanguageManager.Instance.CurrentLanguage switch
            {
                "French" => new CultureInfo("fr-FR"),
                "Arabic" => new CultureInfo("ar-SA"),
                _ => new CultureInfo("en-US")
            };
            
            TopBarDateTimeText.Text = DateTime.Now.ToString("ddd, MMM d  •  HH:mm:ss", culture);
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.OemPlus || e.Key == Key.Add))
            {
                e.Handled = true;
                QuickAction_Click(this, new RoutedEventArgs());
                return;
            }

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Tab)
            {
                e.Handled = true;
                bool backwards = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
                CyclePage(backwards);
            }
        }

        private void CyclePage(bool backwards)
        {
            var currentType = MainFrame.Content?.GetType();
            int currentIndex = currentType is null ? 0 : _pageCycleOrder.IndexOf(currentType);
            if (currentIndex < 0) currentIndex = 0;

            int nextIndex = backwards
                ? (currentIndex - 1 + _pageCycleOrder.Count) % _pageCycleOrder.Count
                : (currentIndex + 1) % _pageCycleOrder.Count;

            NavigateToPageType(_pageCycleOrder[nextIndex]);
        }

        private void NavigateToPageType(System.Type pageType)
        {
            if (pageType == typeof(HomePage)) { HomeButton_Click(this, new RoutedEventArgs()); return; }
            if (pageType == typeof(PatientsPage)) { PatientsButton_Click(this, new RoutedEventArgs()); return; }
            if (pageType == typeof(AppointmentsPage)) { AppointmentsButton_Click(this, new RoutedEventArgs()); return; }
            if (pageType == typeof(PrescriptionPage)) { PrescriptionButton_Click(this, new RoutedEventArgs()); return; }
            if (pageType == typeof(Settings)) { SettingsButton_Click(this, new RoutedEventArgs()); return; }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                maximize_button.Content = "🗖";
            }
            else
            {
                WindowState = WindowState.Maximized;
                maximize_button.Content = "🗗";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle)
            {
                ThemeManager.Instance.SetTheme(toggle.IsChecked == true);
            }
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (LanguageSelector?.SelectedIndex == null || LanguageSelector.SelectedIndex < 0) return;

            LanguageManager.Instance.SetLanguageByIndex(LanguageSelector.SelectedIndex);
        }

        private void SetActiveNav(Button active)
        {
            NavHomeButton.IsDefault = false;
            NavPatientsButton.IsDefault = false;
            NavAppointmentsButton.IsDefault = false;
            NavPrescriptionButton.IsDefault = false;
            NavConsultationButton.IsDefault = false;
            NavSettingsButton.IsDefault = false;
            active.IsDefault = true;
        }

        public void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavHomeButton);
            MainFrame.Navigate(new HomePage());
        }

        public void PatientsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavPatientsButton);
            MainFrame.Navigate(new PatientsPage());
        }

        public void AppointmentsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavAppointmentsButton);
            MainFrame.Navigate(new AppointmentsPage());
        }

        private void PrescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavPrescriptionButton);
            MainFrame.Navigate(new PrescriptionPage());
        }

        private void ConsultationButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavConsultationButton);
            MainFrame.Navigate(new ConsultationPage());
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavSettingsButton);
            MainFrame.Navigate(new Settings());
        }

        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new QuickActionWindow();
            dialog.Owner = this;
                dialog.ShowDialog();
        }

        public void RefreshCurrentPage()
        {
            var currentType = MainFrame.Content?.GetType();
            if (currentType == typeof(HomePage)) { MainFrame.Navigate(new HomePage()); return; }
            if (currentType == typeof(PatientsPage)) { MainFrame.Navigate(new PatientsPage()); return; }
            if (currentType == typeof(AppointmentsPage)) { MainFrame.Navigate(new AppointmentsPage()); return; }
            if (currentType == typeof(PrescriptionPage)) { MainFrame.Navigate(new PrescriptionPage()); return; }
            if (currentType == typeof(ConsultationPage)) { MainFrame.Navigate(new ConsultationPage()); return; }
            if (currentType == typeof(Settings)) { MainFrame.Navigate(new Settings()); return; }
        }

        private void MainFrame_Navigated(object? sender, NavigationEventArgs e)
        {
            switch (e.Content)
            {
                case HomePage: SetActiveNav(NavHomeButton); break;
                case PatientsPage: SetActiveNav(NavPatientsButton); break;
                case AppointmentsPage: SetActiveNav(NavAppointmentsButton); break;
                case PrescriptionPage: SetActiveNav(NavPrescriptionButton); break;
                case ConsultationPage: SetActiveNav(NavConsultationButton); break;
                case Settings: SetActiveNav(NavSettingsButton); break;
            }
        }
    }
}
