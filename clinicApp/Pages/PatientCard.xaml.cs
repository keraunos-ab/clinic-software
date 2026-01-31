using clinicApp.Models;
using clinicApp.data;
using System.Windows;
using System.Windows.Controls;

namespace clinicApp
{
    public partial class PatientCard : UserControl
    {
        static private DataBaseManager db;
        private bool _isEditing;

        public PatientCard()
        {
            InitializeComponent();
            SetEditingState(false);
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

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void saveEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditing)
            {
                SetEditingState(true);
                PhoneTextBox.Focus();
                PhoneTextBox.SelectAll();
                return;
            }

            Phone = PhoneTextBox.Text;
            Email = EmailTextBox.Text;
            Note = NoteTextBox.Text;

            if (DataContext is Patient patient)
            {
                patient.Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text;
                patient.Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text;
                patient.Note = string.IsNullOrWhiteSpace(NoteTextBox.Text) ? null : NoteTextBox.Text;

                var db = new DataBaseManager();
                db.UpdatePatientByID(
                    patient.Id,
                    patient.FirstName,
                    patient.LastName,
                    patient.Phone ?? string.Empty,
                    patient.Email ?? string.Empty,
                    patient.Note
                );
            }

            SetEditingState(false);
        }

        private void SetEditingState(bool isEditing)
        {
            _isEditing = isEditing;
            saveEditButton.Content = isEditing ? "Save" : "Edit";

            PhoneTextBox.IsReadOnly = !isEditing;
            EmailTextBox.IsReadOnly = !isEditing;
            NoteTextBox.IsReadOnly = !isEditing;
        }
    }
}
