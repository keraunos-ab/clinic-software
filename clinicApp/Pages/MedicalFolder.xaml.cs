using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using clinicApp.data;
using clinicApp.Models;

namespace clinicApp.Pages
{
    /// <summary>
    /// Interaction logic for MedicalFolder.xaml
    /// </summary>
    public partial class MedicalFolder : Page
    {
        private int _activeMotifIndex = 0;

        private readonly List<MotifData> _motifs = new()
        {
            new MotifData
            {
                PatientName = "Patient firstname lastname 's medical record",
                EtatClinique = "Diabetique",
                Medications = new[] { "medication A", "medication B", "medication C", "medication D" },
                Antecedents = new[] { "Random surgery on dd/mm/yyyy", "Blank accident on dd/mm/yyyy caused x y z injuries/illnesses", "jsp cha ynejem ykon antecedent lssl" },
                ResultatBilan = "jsp franchement cha yji hna",
                DescriptionDiag = "voila hbibna 3ndh kda mena melhih khasah techrolah caviar w chwiya crevette chwiya calamar, katrolah l7am."
            },
            new MotifData
            {
                PatientName = "Patient firstname lastname 's medical record",
                EtatClinique = "Hypertendu",
                Medications = new[] { "Amlodipine 5mg", "Lisinopril 10mg" },
                Antecedents = new[] { "Appendectomy on 15/03/2015", "Fractured wrist on 22/08/2019" },
                ResultatBilan = "Tension arterielle 14/9 stable",
                DescriptionDiag = "Le patient presente une hypertension arterielle controlee sous traitement. Continuer le suivi mensuel et adapter la posologie si necessaire."
            }
        };

        private readonly int _patientId;
        private readonly string _firstName;
        private readonly string _lastName;

        public MedicalFolder() : this(0, "firstname", "lastname") { }

        public MedicalFolder(int patientId, string firstName, string lastName)
        {
            _patientId = patientId;
            _firstName = firstName;
            _lastName = lastName;
            InitializeComponent();

            // Update the hard-coded motif headers with the real patient name
            foreach (var motif in _motifs)
                motif.PatientName = $"{_firstName} {_lastName} 's medical record";

            LoadMotif(0);
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var db = new DataBaseManager();
            var sessions = db.GetSessionsByPatient(_patientId);
            var historyWindow = new PatientHistoryWindow(_firstName, _lastName, sessions);
            historyWindow.Owner = Window.GetWindow(this);
            historyWindow.ShowDialog();
        }

        private void MotifTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out int index))
            {
                LoadMotif(index);
            }
        }

        private void LoadMotif(int index)
        {
            if (index < 0 || index >= _motifs.Count) return;
            _activeMotifIndex = index;
            var data = _motifs[index];

            // Swap tab styles
            TabMotifA.Style = (Style)Resources[index == 0 ? "MotifTabButtonActive" : "MotifTabButton"];
            TabMotifB.Style = (Style)Resources[index == 1 ? "MotifTabButtonActive" : "MotifTabButton"];

            // Header
            PatientNameHeader.Text = data.PatientName;
            EtatCliniqueText.Text = data.EtatClinique;

            // Medications
            TextBlock[] meds = { Med1, Med2, Med3, Med4 };
            for (int i = 0; i < meds.Length; i++)
            {
                if (i < data.Medications.Length)
                {
                    meds[i].Text = data.Medications[i];
                    meds[i].Visibility = Visibility.Visible;
                }
                else
                {
                    meds[i].Visibility = Visibility.Collapsed;
                }
            }

            // Antecedents
            TextBlock[] ants = { Ant1, Ant2, Ant3 };
            for (int i = 0; i < ants.Length; i++)
            {
                if (i < data.Antecedents.Length)
                {
                    ants[i].Text = data.Antecedents[i];
                    ants[i].Visibility = Visibility.Visible;
                }
                else
                {
                    ants[i].Visibility = Visibility.Collapsed;
                }
            }

            // Text fields
            ResultatBilanText.Text = data.ResultatBilan;
            DescriptionDiagText.Text = data.DescriptionDiag;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private void AddMotifBtn_Click(object sender, RoutedEventArgs e)
        {
            var addMotivWindow = new AddMotiv(_firstName, _lastName);
            addMotivWindow.Owner = Window.GetWindow(this);
            addMotivWindow.ShowDialog();
        }

        private class MotifData
        {
            public string PatientName { get; set; } = "";
            public string EtatClinique { get; set; } = "";
            public string[] Medications { get; set; } = Array.Empty<string>();
            public string[] Antecedents { get; set; } = Array.Empty<string>();
            public string ResultatBilan { get; set; } = "";
            public string DescriptionDiag { get; set; } = "";
        }
    }
}
