using clinicApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace clinicApp
{
    public partial class PatientCard : UserControl
    {
        public PatientCard()
        {
            InitializeComponent();
        }

        public string FirstName
        {
            get => (string)GetValue(FirstNameProperty);
            set => SetValue(FirstNameProperty, value);
        }
        public static readonly DependencyProperty FirstNameProperty =
            DependencyProperty.Register(nameof(FirstName), typeof(string), typeof(PatientCard), new PropertyMetadata(""));

        public string LastName
        {
            get => (string)GetValue(LastNameProperty);
            set => SetValue(LastNameProperty, value);
        }
        public static readonly DependencyProperty LastNameProperty =
            DependencyProperty.Register(nameof(LastName), typeof(string), typeof(PatientCard), new PropertyMetadata(""));

        public string Phone
        {
            get => (string)GetValue(PhoneProperty);
            set => SetValue(PhoneProperty, value);
        }
        public static readonly DependencyProperty PhoneProperty =
            DependencyProperty.Register(nameof(Phone), typeof(string), typeof(PatientCard), new PropertyMetadata(""));

        public string Email
        {
            get => (string)GetValue(EmailProperty);
            set => SetValue(EmailProperty, value);
        }
        public static readonly DependencyProperty EmailProperty =
            DependencyProperty.Register(nameof(Email), typeof(string), typeof(PatientCard), new PropertyMetadata(""));

        public string Note
        {
            get => (string)GetValue(NoteProperty);
            set => SetValue(NoteProperty, value);
        }
        public static readonly DependencyProperty NoteProperty =
            DependencyProperty.Register(nameof(Note), typeof(string), typeof(PatientCard), new PropertyMetadata(""));

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is Patient patient)
            {
                var db = new DataBaseManager();
                var historyWindow = new PatientHistoryWindow(patient.FirstName, patient.LastName, db.GetSessionsByPatient(patient.Id));
                historyWindow.Owner = Window.GetWindow(this);
                historyWindow.ShowDialog();
            }
        }

    }
}
