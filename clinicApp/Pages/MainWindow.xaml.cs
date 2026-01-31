using clinicApp.data;
using clinicApp.Pages;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
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
        private const string CredentialsDbFileName = "UserCredentials.db";
        private const string CredentialsTableName = "UserCredentials";

        private readonly DispatcherTimer _topBarClockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private bool _isDarkTheme = false;

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
                    $"SELECT name FROM sqlite_master WHERE type='table' AND name=@t LIMIT 1;",
                    conn);
                cmd.Parameters.AddWithValue("@t", CredentialsTableName);

                var tableExists = cmd.ExecuteScalar() is not null;
                if (!tableExists)
                    return true;

                using var countCmd = new SQLiteCommand($"SELECT COUNT(*) FROM {CredentialsTableName};", conn);
                var count = Convert.ToInt64(countCmd.ExecuteScalar());

                return count == 0;
            }
            catch
            {
                // If anything is off (corrupt DB, etc.), treat as first run to recover via onboarding.
                return true;
            }
        }

        private void UpdateTopBarDateTime()
        {
            // Example: "Sat, Jan 24 • 14:05:09"
            TopBarDateTimeText.Text = DateTime.Now.ToString("ddd, MMM d  •  HH:mm:ss");
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl + '+' (often comes as Key.OemPlus with Shift)
            // Also support Ctrl + '=' (same key without Shift on US layouts)
            if (Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.OemPlus || e.Key == Key.Add))
            {
                e.Handled = true;
                QuickAction_Click(this, new RoutedEventArgs());
                return;
            }

            // Ctrl+Tab / Ctrl+Shift+Tab : cycle pages
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
            if (pageType == typeof(HomePage))
            {
                HomeButton_Click(this, new RoutedEventArgs());
                return;
            }

            if (pageType == typeof(PatientsPage))
            {
                PatientsButton_Click(this, new RoutedEventArgs());
                return;
            }

            if (pageType == typeof(AppointmentsPage))
            {
                AppointmentsButton_Click(this, new RoutedEventArgs());
                return;
            }

            if (pageType == typeof(PrescriptionPage))
            {
                PrescriptionButton_Click(this, new RoutedEventArgs());
                return;
            }

            if (pageType == typeof(Settings))
            {
                SettingsButton_Click(this, new RoutedEventArgs());
                return;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if(WindowState == WindowState.Maximized)
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
                _isDarkTheme = toggle.IsChecked == true;
                Application.Current.Resources.MergedDictionaries.Clear();
                var themeUri = new Uri(_isDarkTheme ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
            }
        }

        private void SetActiveNav(Button active)
        {
            NavHomeButton.IsDefault = false;
            NavPatientsButton.IsDefault = false;
            NavAppointmentsButton.IsDefault = false;
            NavPrescriptionButton.IsDefault = false;
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

        private void MainFrame_Navigated(object? sender, NavigationEventArgs e)
        {
            switch (e.Content)
            {
                case HomePage:
                    SetActiveNav(NavHomeButton);
                    break;
                case PatientsPage:
                    SetActiveNav(NavPatientsButton);
                    break;
                case AppointmentsPage:
                    SetActiveNav(NavAppointmentsButton);
                    break;
                case PrescriptionPage:
                    SetActiveNav(NavPrescriptionButton);
                    break;
                case Settings:
                    SetActiveNav(NavSettingsButton);
                    break;
            }
        }
    }
}
