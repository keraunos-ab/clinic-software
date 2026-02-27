using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using clinicApp.data;
using clinicApp.Models;

namespace clinicApp
{
    public partial class PrescriptionPage : Page
    {
        private sealed class PrescriptionPageViewModel
        {
            public ImageSource? UserLogoImage { get; init; }
        }

        private readonly DataBaseManager db = new DataBaseManager();

        private StackPanel? _medicineList;

        private List<PatientDisplay> _allPatients = new();
        private bool _suppressSearch;

        public PrescriptionPage()
        {
            InitializeComponent();

            _medicineList = FindName("MedicineList") as StackPanel;
            DateInput.Text = DateTime.Now.ToString("yyyy-MM-dd");
            Doctor dr = db.GetDoctorCredentials();
            ClinicTitle.Text = dr.getClinicName();
            DoctorNameText.Text = "Dr " + dr.getFirstName() + " " + dr.getLastName();
            Specialty.Text = dr.getSpecialty();
            N_order.Text = "N° D'ordre: " + dr.getN_dordre();
            phone.Text = dr.getPhoneNumber();
            mail.Text = dr.getEmail();
            adress.Text = dr.getClinicAddress();

            ImageSource? logo = null;
            var logoPath = dr.getLogoPath();
            if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(logoPath, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                logo = bmp;
            }
            DataContext = new PrescriptionPageViewModel { UserLogoImage = logo };

            Loaded += PrescriptionPage_Loaded;
        }

        private void PrescriptionPage_Loaded(object sender, RoutedEventArgs e)
        {
            var patients = db.GetAllPatients();
            _allPatients = patients
                .Select(p => new PatientDisplay
                {
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender
                })
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .ToList();
        }

        private void PatientSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressSearch)
                return;

            string query = FirstNameInput.Text.Trim();

            if (string.IsNullOrEmpty(query))
            {
                PatientPopup.IsOpen = false;
                PatientResultsList.ItemsSource = null;
                LastNameInput.Text = "";
                AgeInput.Text = "";
                SexInput.SelectedIndex = 0;
                return;
            }

            var results = _allPatients
                .Where(p =>
                    p.FirstName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    p.LastName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    p.FullName.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            PatientResultsList.ItemsSource = results;
            PatientPopup.IsOpen = results.Count > 0;
        }

        private void PatientResultsList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CommitSelectedPatient();
        }

        private void PatientSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!PatientPopup.IsOpen)
                return;

            if (e.Key == Key.Down)
            {
                PatientResultsList.SelectedIndex =
                    Math.Min(PatientResultsList.SelectedIndex + 1, PatientResultsList.Items.Count - 1);
                PatientResultsList.ScrollIntoView(PatientResultsList.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                PatientResultsList.SelectedIndex =
                    Math.Max(PatientResultsList.SelectedIndex - 1, 0);
                PatientResultsList.ScrollIntoView(PatientResultsList.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                CommitSelectedPatient();
                e.Handled = true;
            }
        }

        private void CommitSelectedPatient()
        {
            if (PatientResultsList.SelectedItem is not PatientDisplay selected)
                return;

            _suppressSearch = true;

            FirstNameInput.Text = selected.FirstName;
            LastNameInput.Text = selected.LastName;

            int age = DateTime.Now.Year - selected.DateOfBirth.Year;
            if (DateTime.Now < selected.DateOfBirth.AddYears(age))
                age--;
            AgeInput.Text = age.ToString();

            SexInput.SelectedIndex = selected.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            PatientPopup.IsOpen = false;
            FirstNameInput.Select(FirstNameInput.Text.Length, 0);

            _suppressSearch = false;
        }

        private void AddMedicineButton_Click(object sender, RoutedEventArgs e)
        {
            if (_medicineList is null) return;

            const int MaxMedicineSlots = 10;
            if (_medicineList.Children.Count >= MaxMedicineSlots)
            {
                MessageBox.Show(
                    $"You can only add up to {MaxMedicineSlots} medicines.",
                    "Limit reached",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var container = new Grid
            {
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });   // Medicament
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) }); // Dosage (wider)
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.4, GridUnitType.Star) }); // N° boites (tighter)
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Poscologie
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                        // Remove

            var searchBox = new TextBox
            {
                Height = 38,
                FontSize = 18,
                Padding = new Thickness(6, 4, 6, 4),
                Background = Brushes.White,
                BorderBrush = Brushes.Black
            };
            Grid.SetColumn(searchBox, 0);

            var popup = new Popup
            {
                PlacementTarget = searchBox,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };

            var resultsBox = new ListBox
            {
                MaxHeight = 200,
                FontSize = 10,
                DisplayMemberPath = "FullDisplay"
            };

            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Child = resultsBox
            };

            popup.Child = border;

            var amountBox = new TextBox
            {
                Height = 38,
                FontSize = 18,
                Padding = new Thickness(6, 4, 6, 4),
                Background = Brushes.White,
                BorderBrush = Brushes.Black
            };
            Grid.SetColumn(amountBox, 1);

            var perDayBox = new TextBox
            {
                Height = 38,
                FontSize = 18,
                Padding = new Thickness(6, 4, 6, 4),
                Background = Brushes.White,
                BorderBrush = Brushes.Black
            };
            Grid.SetColumn(perDayBox, 2);

            var durationBox = new TextBox
            {
                Height = 38,
                FontSize = 18,
                Padding = new Thickness(6, 4, 6, 4),
                Background = Brushes.White,
                BorderBrush = Brushes.Black
            };
            Grid.SetColumn(durationBox, 3);

            var removeButton = new Button
            {
                Content = "×",
                Width = 35,
                Height = 38,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(8, 0, 0, 0),
                Background = Brushes.LightCoral,
                Foreground = Brushes.White
            };
            Grid.SetColumn(removeButton, 4);

            removeButton.Click += (s, args) =>
            {
                if (_medicineList is not null)
                    _medicineList.Children.Remove(container);
            };

            searchBox.TextChanged += (s, args) =>
            {
                string query = searchBox.Text.Trim();

                if (string.IsNullOrEmpty(query))
                {
                    popup.IsOpen = false;
                    amountBox.Text = "";
                    return;
                }

                var results = db.GetMedicinesByPrefix(query);

                resultsBox.ItemsSource = results;
                popup.IsOpen = results.Any();
            };

            // Selection event
            resultsBox.MouseLeftButtonUp += (s, args) =>
            {
                if (resultsBox.SelectedItem is MedicineInfo med)
                {
                    searchBox.Text = med.Name;
                    amountBox.Text = med.Dosage;
                    popup.IsOpen = false;
                    searchBox.Select(searchBox.Text.Length, 0);
                }
            };

            // Keyboard navigation
            searchBox.PreviewKeyDown += (s, args) =>
            {
                if (!popup.IsOpen)
                    return;

                if (args.Key == Key.Down)
                {
                    resultsBox.SelectedIndex =
                        Math.Min(resultsBox.SelectedIndex + 1, resultsBox.Items.Count - 1);
                    resultsBox.ScrollIntoView(resultsBox.SelectedItem);
                    args.Handled = true;
                }
                else if (args.Key == Key.Up)
                {
                    resultsBox.SelectedIndex =
                        Math.Max(resultsBox.SelectedIndex - 1, 0);
                    resultsBox.ScrollIntoView(resultsBox.SelectedItem);
                    args.Handled = true;
                }
                else if (args.Key == Key.Enter && resultsBox.SelectedItem is MedicineInfo med)
                {
                    searchBox.Text = med.Name;
                    amountBox.Text = med.Dosage;
                    popup.IsOpen = false;
                    searchBox.Select(searchBox.Text.Length, 0);
                    args.Handled = true;
                }
            };

            container.Children.Add(searchBox);
            container.Children.Add(amountBox);
            container.Children.Add(perDayBox);
            container.Children.Add(durationBox);
            container.Children.Add(removeButton);

            _medicineList.Children.Add(container);
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            if (_medicineList is null) return;

            AddMedicineButton.Visibility = Visibility.Collapsed;
            FinishButton.Visibility = Visibility.Collapsed;

            foreach (var child in _medicineList.Children)
            {
                if (child is Panel panel)
                {
                    foreach (var item in panel.Children)
                    {
                        if (item is Button btn)
                            btn.Visibility = Visibility.Collapsed;
                    }
                }
            }

            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                PrintableArea.Measure(new Size(pd.PrintableAreaWidth, pd.PrintableAreaHeight));
                PrintableArea.Arrange(new Rect(new Point(0, 0), PrintableArea.DesiredSize));

                pd.PrintVisual(PrintableArea, "Prescription");
            }

            AddMedicineButton.Visibility = Visibility.Visible;
            FinishButton.Visibility = Visibility.Visible;

            foreach (var child in _medicineList.Children)
            {
                if (child is Panel panel)
                {
                    foreach (var item in panel.Children)
                    {
                        if (item is Button btn)
                            btn.Visibility = Visibility.Visible;
                    }
                }
            }
        }
    }
}