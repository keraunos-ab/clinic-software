using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace clinicApp.Pages
{
    public partial class AddMotiv : Window
    {
        public AddMotiv(string patientFirstName, string patientLastName)
        {
            InitializeComponent();
            Title = $"{patientFirstName} {patientLastName}";
        }

        private void AddMedBtn_Click(object sender, RoutedEventArgs e)
        {
            AddEntryToPanel(MedicationsPanel);
        }

        private void AddAntBtn_Click(object sender, RoutedEventArgs e)
        {
            AddEntryToPanel(AntecedentsPanel);
        }

        private void AddEntryToPanel(StackPanel panel)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };

            var entryBox = new TextBox
            {
                Style = (Style)FindResource("EntryInputBox"),
                MinWidth = 300
            };

            var removeBtn = new Button
            {
                Style = (Style)FindResource("RemoveEntryButton")
            };
            removeBtn.Click += (s, args) => panel.Children.Remove(row);

            row.Children.Add(entryBox);
            row.Children.Add(removeBtn);
            panel.Children.Add(row);
            entryBox.Focus();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // TODO: persist motiv data
            MessageBox.Show("Saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
