using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using clinicApp.data;

namespace clinicApp.Pages
{
    public partial class AddMotiv : Window
    {
        private readonly int _patientId;
        private readonly DataBaseManager _db = new();

        public bool Saved { get; private set; }

        public AddMotiv(int patientId, string patientFirstName, string patientLastName)
        {
            _patientId = patientId;
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

        private void ParcourirBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Bilan Picture",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                BilanFilePathText.Text = openFileDialog.FileName;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string date = DateTime.Now.ToString("yyyy-MM-dd");
                string? motiv = string.IsNullOrWhiteSpace(Motiv.Text) ? null : Motiv.Text.Trim();

                byte[]? bilanBytes = null;
                string bilanPath = BilanFilePathText.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(bilanPath))
                    bilanBytes = ImageHelper.ConvertToWebP(bilanPath, 80);

                string[]? antecedents = CollectEntries(AntecedentsPanel);
                string[]? medications = CollectEntries(MedicationsPanel);

                string? hdm = string.IsNullOrWhiteSpace(HDMText.Text) ? null : HDMText.Text.Trim();
                string? etatClinique = string.IsNullOrWhiteSpace(EtatCliniqueText.Text) ? null : EtatCliniqueText.Text.Trim();
                string? cat = string.IsNullOrWhiteSpace(CATText.Text) ? null : CATText.Text.Trim();

                _db.AddConsultation(_patientId, date, motiv, bilanBytes, antecedents, medications, hdm, etatClinique, cat);

                Saved = true;
                MessageBox.Show("Saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving consultation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string[]? CollectEntries(StackPanel panel)
        {
            var entries = new List<string>();
            foreach (var child in panel.Children)
            {
                if (child is StackPanel row)
                {
                    var textBox = row.Children.OfType<TextBox>().FirstOrDefault();
                    if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
                        entries.Add(textBox.Text.Trim());
                }
            }
            return entries.Count > 0 ? entries.ToArray() : null;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
