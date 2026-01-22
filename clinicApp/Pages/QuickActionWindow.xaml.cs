using System.Windows;

namespace clinicApp
{
    public partial class QuickActionWindow : Window
    {
        public QuickActionWindow()
        {
            InitializeComponent();

            // Load each tab’s content (separate pages for clarity)
            PatientFrame.Navigate(new AddPatientPage());
            AppointmentFrame.Navigate(new AddApointmentPage());
            SessionFrame.Navigate(new AddSessionPage());
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
