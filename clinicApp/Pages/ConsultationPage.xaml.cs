using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using clinicApp.data;
using clinicApp.Models;
using clinicApp.Services;

namespace clinicApp.Pages
{
    public partial class ConsultationPage : Page
    {
        private int _activeConsultationIndex = 0;
        private int _patientId = 0;
        private string _firstName = "";
        private string _lastName = "";
        internal MainWindow Owner;
        private readonly DataBaseManager _db = new();
        private List<Consultation> _consultations = new();

        public ConsultationPage()
        {
            InitializeComponent();

            Loaded += (s, e) => LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
            Unloaded += (s, e) => LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
        }

        // ── Search ──────────────────────────────────────────────────────────

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(query))
            {
                SearchPopup.IsOpen = false;
                return;
            }

            var results = _db.GetPatientsByPrefix(query);

            if (results.Count == 0)
            {
                SearchPopup.IsOpen = false;
                return;
            }

            SearchResultsList.Tag = results;
            SearchResultsList.ItemsSource = results.Select(p => $"{p.FirstName} {p.LastName}").ToList();
            SearchPopup.IsOpen = true;
        }

        private void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResultsList.SelectedIndex < 0) return;

            var patients = SearchResultsList.Tag as List<Patient>;
            if (patients == null || SearchResultsList.SelectedIndex >= patients.Count) return;

            var patient = patients[SearchResultsList.SelectedIndex];
            _patientId = patient.Id;
            _firstName = patient.FirstName;
            _lastName = patient.LastName;

            SearchTextBox.Text = $"{patient.FirstName} {patient.LastName}";
            SearchPopup.IsOpen = false;
            SearchResultsList.SelectedIndex = -1;

            LoadPatientConsultations();
        }

        // ── Birthdate Search ─────────────────────────────────────────────────

        private void BirthdateSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = BirthdateSearchTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(query))
            {
                BirthdateSearchPopup.IsOpen = false;
                return;
            }

            // Search for patients with matching birthdate
            var results = _db.GetPatientsByBirthdate(query);

            if (results.Count == 0)
            {
                BirthdateSearchPopup.IsOpen = false;
                return;
            }

            BirthdateSearchResultsList.Tag = results;
            BirthdateSearchResultsList.ItemsSource = results.Select(p => 
                $"{p.FirstName} {p.LastName} ({p.DateOfBirth:dd/MM/yyyy})").ToList();
            BirthdateSearchPopup.IsOpen = true;
        }

        private void BirthdateSearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BirthdateSearchResultsList.SelectedIndex < 0) return;

            var patients = BirthdateSearchResultsList.Tag as List<Patient>;
            if (patients == null || BirthdateSearchResultsList.SelectedIndex >= patients.Count) return;

            var patient = patients[BirthdateSearchResultsList.SelectedIndex];
            _patientId = patient.Id;
            _firstName = patient.FirstName;
            _lastName = patient.LastName;

            BirthdateSearchTextBox.Text = patient.DateOfBirth.ToString("dd/MM/yyyy");
            BirthdateSearchPopup.IsOpen = false;
            BirthdateSearchResultsList.SelectedIndex = -1;

            LoadPatientConsultations();
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            if (ConsultationArea.Visibility == Visibility.Visible)
            {
                UpdatePatientTitle();
            }
        }

        private void UpdatePatientTitle()
        {
            string titleResource = TryFindResource("MedicalFolderTitle") as string ?? "'s medical record";
            string patientName = $"{_firstName} {_lastName}";
            string fullTitle = titleResource.StartsWith("'") || titleResource.StartsWith("السجل") 
                ? $"{patientName}{titleResource}" 
                : $"{titleResource} {patientName}";

            PatientNameHeader.Text = fullTitle;
        }

        private void LoadPatientConsultations()
        {
            _consultations = _db.GetConsultationsByPatient(_patientId);

            UpdatePatientTitle();
            UpdateTabs();

            EmptyState.Visibility = Visibility.Collapsed;
            ConsultationArea.Visibility = Visibility.Visible;

            if (_consultations.Count > 0)
                LoadConsultation(0);
        }

        private void UpdateTabs()
        {
            // Remove all dynamic tab buttons (everything before the "+" button)
            while (TabBar.Children.Count > 1)
                TabBar.Children.RemoveAt(0);

            for (int i = 0; i < _consultations.Count; i++)
            {
                var btn = new Button
                {
                    Content = _consultations[i].Motiv ?? _consultations[i].Date,
                    Style = (Style)Resources["ConsultationTabButton"],
                    Tag = i.ToString()
                };
                btn.Click += ConsultationTab_Click;
                TabBar.Children.Insert(i, btn);
            }
        }

        // ── Consultation tabs ────────────────────────────────────────────────

        private void ConsultationTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out int index))
                LoadConsultation(index);
        }

        private void LoadConsultation(int index)
        {
            if (index < 0 || index >= _consultations.Count) return;
            _activeConsultationIndex = index;
            var data = _consultations[index];

            // Highlight the active tab
            for (int i = 0; i < TabBar.Children.Count - 1; i++)
            {
                if (TabBar.Children[i] is Button tabBtn)
                    tabBtn.Style = (Style)Resources[i == index ? "ConsultationTabButtonActive" : "ConsultationTabButton"];
            }

            // Motiv
            MotivText.Text = data.Motiv ?? "";

            // Bilan image
            if (data.BilanImage != null && data.BilanImage.Length > 0)
            {
                BilanImage.Source = ImageHelper.ToBitmapImage(data.BilanImage);
                BilanImageBorder.Visibility = Visibility.Visible;
            }
            else
            {
                BilanImage.Source = null;
                BilanImageBorder.Visibility = Visibility.Collapsed;
            }

            // HDM
            HDMText.Text = data.Hdm ?? "";

            // Etat Clinique
            EtatCliniqueText.Text = data.EtatClinique ?? "";

            // CAT
            CATText.Text = data.Cat ?? "";

            // Medications
            PopulateListPanel(MedicationsPanel, data.Medications);

            // Antecedents
            PopulateListPanel(AntecedentsPanel, data.Antecedents);
        }

        private void PopulateListPanel(StackPanel panel, string[]? items)
        {
            panel.Children.Clear();
            if (items == null) return;

            foreach (var item in items)
            {
                var bullet = new BulletDecorator { Margin = new Thickness(28, 3, 0, 3) };
                bullet.Bullet = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = (Brush)FindResource("HintBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                bullet.Child = new TextBlock
                {
                    Text = item,
                    Style = (Style)FindResource("ExpanderItemText")
                };
                panel.Children.Add(bullet);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ConsultationArea.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            SearchTextBox.Text = string.Empty;
        }

        private void ConsultationHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_consultations.Count == 0) return;
            var consultationId = _consultations[_activeConsultationIndex].Id;
            var checkups = _db.GetSessionsByConsultation(consultationId);
            string title = TryFindResource("BtnHistory2") as string ?? "Consultation History";
            var historyWindow = new PatientHistoryWindow(_firstName, _lastName, checkups, title);
            historyWindow.Owner = Window.GetWindow(this);
            historyWindow.ShowDialog();
        }

        private void FullHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var checkups = _db.GetSessionsByPatient(_patientId);
            var historyWindow = new PatientHistoryWindow(_firstName, _lastName, checkups);
            historyWindow.Owner = Window.GetWindow(this);
            historyWindow.ShowDialog();
        }

        private void AddConsultationBtn_Click(object sender, RoutedEventArgs e)
        {
            var addConsultationWindow = new AddMotiv(_patientId, _firstName, _lastName);
            addConsultationWindow.Owner = Window.GetWindow(this);
            addConsultationWindow.ShowDialog();

            if (addConsultationWindow.Saved)
                LoadPatientConsultations();
        }

        private void BilanImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (BilanImage.Source == null) return;

            var scale = new ScaleTransform(1, 1);
            var translate = new TranslateTransform(0, 0);
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(translate);

            var img = new Image
            {
                Source = BilanImage.Source,
                Stretch = Stretch.Uniform,
                RenderTransform = transformGroup
            };

            var closeBtn = new Button
            {
                Content = "✕",
                FontSize = 18,
                Width = 44,
                Height = 44,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(160, 30, 30, 30)),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 16, 16, 0),
                Cursor = Cursors.Hand
            };

            var hint = new TextBlock
            {
                Text = "Scroll to zoom  •  Drag to pan  •  Double-click to reset",
                Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 20),
                IsHitTestVisible = false
            };

            var grid = new Grid { Background = Brushes.Black, ClipToBounds = true };
            grid.Children.Add(img);
            grid.Children.Add(hint);
            grid.Children.Add(closeBtn);

            var win = new Window
            {
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Maximized,
                Background = Brushes.Black,
                Owner = Window.GetWindow(this),
                Content = grid
            };

            closeBtn.Click += (s, args) => win.Close();
            win.KeyDown += (s, args) => { if (args.Key == Key.Escape) win.Close(); };

            // ── Zoom toward cursor (mouse wheel) ─────────────────────────────
            const double zoomStep = 1.15;
            const double minScale = 0.5;
            const double maxScale = 10.0;

            grid.MouseWheel += (s, args) =>
            {
                double oldScale = scale.ScaleX;
                double newScale = Math.Max(minScale, Math.Min(maxScale,
                    oldScale * (args.Delta > 0 ? zoomStep : 1.0 / zoomStep)));
                double r = newScale / oldScale;
                var mouse = args.GetPosition(grid);
                translate.X = mouse.X * (1 - r) + translate.X * r;
                translate.Y = mouse.Y * (1 - r) + translate.Y * r;
                scale.ScaleX = newScale;
                scale.ScaleY = newScale;
                args.Handled = true;
            };

            // ── Pan (drag) + double-click to reset ───────────────────────────
            Point dragStart = default;
            double startTx = 0, startTy = 0;
            bool isDragging = false;

            grid.MouseLeftButtonDown += (s, args) =>
            {
                var src = args.OriginalSource as DependencyObject;
                while (src != null) { if (src == closeBtn) return; src = VisualTreeHelper.GetParent(src); }

                if (args.ClickCount == 2)
                {
                    scale.ScaleX = 1; scale.ScaleY = 1;
                    translate.X = 0; translate.Y = 0;
                    args.Handled = true;
                    return;
                }

                dragStart = args.GetPosition(grid);
                startTx = translate.X;
                startTy = translate.Y;
                isDragging = true;
                grid.CaptureMouse();
                grid.Cursor = Cursors.SizeAll;
                args.Handled = true;
            };

            grid.MouseMove += (s, args) =>
            {
                if (!isDragging || args.LeftButton != MouseButtonState.Pressed) return;
                var pos = args.GetPosition(grid);
                translate.X = startTx + (pos.X - dragStart.X);
                translate.Y = startTy + (pos.Y - dragStart.Y);
            };

            grid.MouseLeftButtonUp += (s, args) =>
            {
                isDragging = false;
                grid.ReleaseMouseCapture();
                grid.Cursor = Cursors.Arrow;
            };

            win.ShowDialog();
        }
    }
}

