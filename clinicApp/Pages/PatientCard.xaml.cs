using clinicApp.Models;
using clinicApp.data;
using clinicApp.Services;
using System.Windows;
using System.Windows.Controls;

namespace clinicApp
{
    public partial class PatientCard : UserControl
    {
        private bool _isEditing;

        public PatientCard()
        {
            InitializeComponent();
            SetEditingState(false);
        }

        public int PatientId
        {
            get => (int)GetValue(PatientIdProperty);
            set => SetValue(PatientIdProperty, value);
        }
        public static readonly DependencyProperty PatientIdProperty =
            DependencyProperty.Register(nameof(PatientId), typeof(int), typeof(PatientCard), new PropertyMetadata(0));

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

        public DateTime DateOfBirth
        {
            get => (DateTime)GetValue(DateOfBirthProperty);
            set => SetValue(DateOfBirthProperty, value);
        }
        public static readonly DependencyProperty DateOfBirthProperty =
            DependencyProperty.Register(nameof(DateOfBirth), typeof(DateTime), typeof(PatientCard), new PropertyMetadata(default(DateTime)));

        public string Gender
        {
            get => (string)GetValue(GenderProperty);
            set => SetValue(GenderProperty, value);
        }
        public static readonly DependencyProperty GenderProperty =
            DependencyProperty.Register(nameof(Gender), typeof(string), typeof(PatientCard), new PropertyMetadata(""));

        public double? Weight
        {
            get => (double?)GetValue(WeightProperty);
            set => SetValue(WeightProperty, value);
        }
        public static readonly DependencyProperty WeightProperty =
            DependencyProperty.Register(nameof(Weight), typeof(double?), typeof(PatientCard), new PropertyMetadata(null));

        public string BloodType
        {
            get => (string)GetValue(BloodTypeProperty);
            set => SetValue(BloodTypeProperty, value);
        }
        public static readonly DependencyProperty BloodTypeProperty =
            DependencyProperty.Register(nameof(BloodType), typeof(string), typeof(PatientCard), new PropertyMetadata(""));

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is Patient patient)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    var frame = mainWindow.FindName("MainFrame") as System.Windows.Controls.Frame;
                    frame?.Navigate(new Pages.MedicalFolder(patient.Id, patient.FirstName, patient.LastName));
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete patient {FirstName} {LastName}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var db = new DataBaseManager();
                db.deletePatient(FirstName, LastName);

                // Refresh the current page
                PageRefreshService.RefreshCurrentPage();
            }
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
                    patient.Gender,
                    patient.Note,
                    patient.DateOfBirth,
                    patient.weight,
                    patient.BloodType
                );
            }

            SetEditingState(false);
        }

        private void SetEditingState(bool isEditing)
        {
            _isEditing = isEditing;
            saveEditButton.SetResourceReference(ContentProperty, isEditing ? "BtnSave" : "BtnEdit");

            PhoneTextBox.IsReadOnly = !isEditing;
            EmailTextBox.IsReadOnly = !isEditing;
            NoteTextBox.IsReadOnly = !isEditing;
        }
    }
}
