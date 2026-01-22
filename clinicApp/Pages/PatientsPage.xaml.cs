using System.Windows.Controls;
using clinicApp.Models;
using System.Collections.Generic;
using clinicApp.data;

namespace clinicApp
{
    public partial class PatientsPage : Page
    {
        private readonly DataBaseManager _db;

        public PatientsPage()
        {
            InitializeComponent();
            _db = new DataBaseManager();
            LoadPatients();
        }

        private void LoadPatients()
        {
            List<Patient> patients = _db.GetAllPatients();
            PatientsList.ItemsSource = patients;
        }
    }
}
