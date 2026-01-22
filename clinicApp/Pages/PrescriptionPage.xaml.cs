using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using clinicApp.data;
using clinicApp.Models;

namespace clinicApp
{
    public partial class PrescriptionPage : Page
    {
        private readonly DataBaseManager db = new DataBaseManager();

        private StackPanel? _medicineList;

        public PrescriptionPage()
        {
            InitializeComponent();

            // Resolve the named element at runtime
            _medicineList = FindName("MedicineList") as StackPanel;

            DateInput.Text = DateTime.Now.ToString("yyyy-MM-dd");
        }

        private void AddMedicineButton_Click(object sender, RoutedEventArgs e)
        {
            if (_medicineList is null) return;

            var container = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 10)
            };

            // Create search textbox
            var searchBox = new TextBox
            {
                Width = 200,
                Height = 38,
                FontSize = 16
            };

            // Create popup for results
            var popup = new Popup
            {
                PlacementTarget = searchBox,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };

            // Create results listbox
            var resultsBox = new ListBox
            {
                Width = 400,
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
                Width = 80,
                Height = 38,
                FontSize = 16,
                Margin = new Thickness(8, 0, 8, 0),
                IsReadOnly = true,
                Background = Brushes.WhiteSmoke
            };

            var perDayBox = new TextBox
            {
                Width = 80,
                Height = 38,
                FontSize = 16,
                Margin = new Thickness(8, 0, 8, 0)
            };

            var durationBox = new TextBox
            {
                Width = 100,
                Height = 38,
                FontSize = 16,
                Margin = new Thickness(8, 0, 8, 0)
            };

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
            removeButton.Click += (s, args) =>
            {
                if (_medicineList is not null)
                    _medicineList.Children.Remove(container);
            };

            // TextChanged event for search
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

            foreach (StackPanel sp in _medicineList.Children)
            {
                foreach (var child in sp.Children)
                {
                    if (child is Button btn)
                        btn.Visibility = Visibility.Collapsed;
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

            foreach (StackPanel sp in _medicineList.Children)
            {
                foreach (var child in sp.Children)
                {
                    if (child is Button btn)
                        btn.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
