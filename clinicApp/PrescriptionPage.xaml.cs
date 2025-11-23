using System.Windows;
using System.Windows.Controls;

namespace clinicApp
{
    public partial class PrescriptionPage : Page
    {
        public PrescriptionPage()
        {
            InitializeComponent();
            DateInput.Text = System.DateTime.Now.ToString("yyyy-MM-dd");
        }

        private void AddMedicineButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if the container is already "full"
            MedicineList.Measure(new Size(MedicineScroll.ActualWidth, double.PositiveInfinity));
            double totalHeight = MedicineList.DesiredSize.Height;
            double availableHeight = MedicineScroll.ViewportHeight > 0 ? MedicineScroll.ViewportHeight : MedicineScroll.Height;

            if (totalHeight >= availableHeight * 0.8)
            {
                MessageBox.Show("Cannot add more medicines — the container is full.");
                return;
            }

            var container = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 10)
            };

            // TextBoxes
            var nameBox = new TextBox
            {
                Width = 200,
                Height = 38,
                FontSize = 16,
                Margin = new Thickness(8, 0, 8, 0)
            };
            var amountBox = new TextBox
            {
                Width = 80,
                Height = 38,
                FontSize = 16,
                Margin = new Thickness(8, 0, 8, 0)
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

            // Remove button
            var removeButton = new Button
            {
                Content = "×",
                Width = 35,
                Height = 38,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(8, 0, 0, 0),
                Background = System.Windows.Media.Brushes.LightCoral,
                Foreground = System.Windows.Media.Brushes.White
            };
            removeButton.Click += (s, args) =>
            {
                MedicineList.Children.Remove(container);
            };

            // Add children to container
            container.Children.Add(nameBox);
            container.Children.Add(amountBox);
            container.Children.Add(perDayBox);
            container.Children.Add(durationBox);
            container.Children.Add(removeButton);

            MedicineList.Children.Add(container);
        }



        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide buttons before printing
            AddMedicineButton.Visibility = Visibility.Collapsed;
            FinishButton.Visibility = Visibility.Collapsed;

            foreach (StackPanel sp in MedicineList.Children)
            {
                // Hide remove buttons for printing
                foreach (var child in sp.Children)
                {
                    if (child is Button btn)
                        btn.Visibility = Visibility.Collapsed;
                }
            }

            // Print dialog
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                // Measure and arrange before printing
                PrintableArea.Measure(new Size(pd.PrintableAreaWidth, pd.PrintableAreaHeight));
                PrintableArea.Arrange(new Rect(new Point(0, 0), PrintableArea.DesiredSize));

                pd.PrintVisual(PrintableArea, "Prescription");
            }

            // Restore buttons after printing
            AddMedicineButton.Visibility = Visibility.Visible;
            FinishButton.Visibility = Visibility.Visible;
            foreach (StackPanel sp in MedicineList.Children)
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
