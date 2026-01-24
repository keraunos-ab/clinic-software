using System.Windows;

namespace clinicApp.Pages
{
    public partial class IntroductionWindow : Window
    {
        public IntroductionWindow()
        {
            InitializeComponent();
            RootFrame.Navigate(new Introduction());
        }

        public void FinishAndOpenMain()
        {
            var main = new clinicApp.MainWindow();
            Application.Current.MainWindow = main;
            main.Show();
            Close();
        }
    }
}