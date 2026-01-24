using clinicApp.data;
using clinicApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace clinicApp
{
    public partial class PatientsPage : Page
    {
        private readonly DataBaseManager _db;
        private List<Patient> _allPatients = new();

        public PatientsPage()
        {
            InitializeComponent();
            _db = new DataBaseManager();
            LoadPatients();
        }

        private void LoadPatients()
        {
            _allPatients = _db.GetAllPatients();
            ApplyFilter();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            string q = SearchTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(q))
            {
                PatientsList.ItemsSource = _allPatients;
                return;
            }

            string qLower = q.ToLowerInvariant();

            PatientsList.ItemsSource = _allPatients.Where(p =>
                (!string.IsNullOrWhiteSpace(p.FirstName) && p.FirstName.ToLowerInvariant().Contains(qLower)) ||
                (!string.IsNullOrWhiteSpace(p.LastName) && p.LastName.ToLowerInvariant().Contains(qLower))
            ).ToList();
        }
    }
}
