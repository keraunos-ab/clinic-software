using System.Windows;

namespace clinicApp
{
    public partial class QuickActionWindow : Window
    {
        public QuickActionWindow()
        {
            InitializeComponent();
            PatientFrame.Navigate(new AddPatientPage());
            AppointmentFrame.Navigate(new AddApointmentPage());
            SessionFrame.Navigate(new AddSessionPage());

            this.Width = SystemParameters.PrimaryScreenWidth * 0.5;
            this.Height = SystemParameters.PrimaryScreenHeight * 0.8;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
