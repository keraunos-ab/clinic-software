using clinicApp.data;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace clinicApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataBaseManager db = new DataBaseManager();
            db.InitializeDatabase();
            MainFrame.Navigate(new HomePage());
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HomePage());
        }

        public void PatientsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PatientsPage());
        }

        public void AppointmentsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AppointmentsPage());
        }

        private void PrescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PrescriptionPage());
        }

        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new QuickActionWindow();
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {

        }
    }
}
    
