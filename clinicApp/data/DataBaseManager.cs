using clinicApp.Models;
using System.Collections.Generic;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace clinicApp.data
{
    internal class DataBaseManager
    {
        private readonly string _clinicConnectionString;
        private readonly string _medicinesConnectionString;
        private readonly string _userCredentialsConnectionString;

        public int AppointmentDurationMinutes { get; set; } = 30;

        public DataBaseManager(
            string clinicDbPath = "ClinicDB.sqlite",
            string medicinesDbPath = "data\\medicines.db",
            string userCredentialsPath = "UserCredentials.db")
        {
            _clinicConnectionString = $"Data Source={clinicDbPath};Version=3;";

            if (string.IsNullOrWhiteSpace(medicinesDbPath))
                medicinesDbPath = "data\\medicines.db";

            if (!Path.IsPathRooted(medicinesDbPath))
                medicinesDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, medicinesDbPath);

            _medicinesConnectionString = $"Data Source={medicinesDbPath};Version=3;";

            if (string.IsNullOrWhiteSpace(userCredentialsPath))
                userCredentialsPath = "UserCredentials.db";

            if (!Path.IsPathRooted(userCredentialsPath))
                userCredentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, userCredentialsPath);

            _userCredentialsConnectionString = $"Data Source={userCredentialsPath};Version=3;";
        }

        public void InitializeDatabase()
        {
            using var conn = new SQLiteConnection(_clinicConnectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(conn);

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Patients (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    first_name TEXT NOT NULL,
                    last_name TEXT NOT NULL,
                    phone TEXT,
                    email TEXT,
                    note TEXT,
                    CHECK (phone IS NOT NULL OR email IS NOT NULL)
                );";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Appointments (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    patient_id INTEGER NOT NULL REFERENCES Patients(id),
                    date TEXT NOT NULL,
                    time TEXT NOT NULL,
                    note TEXT
                );";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Visits (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    patient_id INTEGER NOT NULL REFERENCES Patients(id),
                    date TEXT NOT NULL,
                    time TEXT NOT NULL,
                    description TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        public List<Patient> GetAllPatients()
        {
            var patients = new List<Patient>();

            using var conn = new SQLiteConnection(_clinicConnectionString);
            conn.Open();

            using var cmd = new SQLiteCommand("SELECT * FROM Patients", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                patients.Add(new Patient
                {
                    Id = Convert.ToInt32(reader["id"]),
                    FirstName = reader["first_name"].ToString() ?? "",
                    LastName = reader["last_name"].ToString() ?? "",
                    Phone = reader["phone"]?.ToString(),
                    Email = reader["email"]?.ToString(),
                    Note = reader["note"]?.ToString()
                });
            }

            return patients;
        }



        public int GetPatientIdByName(string firstName, string lastName)
        {
            string query = "SELECT * FROM Patients WHERE first_name = @FirstName AND last_name = @LastName";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return Convert.ToInt32(reader["id"]);
            }
            else
            {
                MessageBox.Show($"No patient found with name: {firstName} {lastName}",
                "Patient Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
                return -1;
            }
        }

        public void AddPatient(string firstName, string lastName, string phone, string email, string? note = null)
        {
            string checkQuery = "SELECT COUNT(*) FROM Patients WHERE first_name = @FirstName AND last_name = @LastName";
            using (var conn = new SQLiteConnection(_clinicConnectionString))
            using (var checkCmd = new SQLiteCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@FirstName", firstName);
                checkCmd.Parameters.AddWithValue("@LastName", lastName);
                conn.Open();
                long count = (long)checkCmd.ExecuteScalar();
                if (count > 0)
                {
                    throw new Exception("A patient with this first and last name already exists.");
                }
                conn.Close();
            }

            string insertQuery = @"
        INSERT INTO Patients (first_name, last_name, phone, email, note)
        VALUES (@FirstName, @LastName, @Phone, @Email, @Note)";
            using (var conn = new SQLiteConnection(_clinicConnectionString))
            using (var cmd = new SQLiteCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(phone) ? DBNull.Value : phone);
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? DBNull.Value : email);
                cmd.Parameters.AddWithValue("@Note", string.IsNullOrEmpty(note) ? DBNull.Value : note);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }


        public void RemovePatient(int patientId)
        {
            string query = "DELETE FROM Patients WHERE id = @PatientId";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void UpdatePatientByID(int patientId, string firstName, string lastName, string phone, string email, string? note = null)
        {
            string query = @"
                UPDATE Patients
                SET first_name = @FirstName,
                    last_name = @LastName,
                    phone = @Phone,
                    email = @Email,
                    note = @Note
                WHERE id = @PatientId";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(phone) ? DBNull.Value : phone);
            cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? DBNull.Value : email);
            cmd.Parameters.AddWithValue("@Note", string.IsNullOrEmpty(note) ? DBNull.Value : note);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public int GetTodaysSessionCount()
        {
            List<Session> sessions = new();
            string query = "SELECT * FROM visits WHERE date = @Today";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            string today = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            cmd.Parameters.AddWithValue("@Today", today);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sessions.Add(new Session
                {
                    Id = Convert.ToInt32(reader["id"]),
                    PatientId = Convert.ToInt32(reader["patient_id"]),
                    Date = DateOnly.Parse(reader["date"].ToString()!),
                    Time = TimeOnly.Parse(reader["time"].ToString()!),
                    Description = reader["description"]?.ToString()
                });
            }
            return sessions.Count;
        }

        public List<Session> GetSessionsByPatient(int patientId)
        {
            List<Session> sessions = new();
            string query = "SELECT * FROM Visits WHERE patient_id = @PatientId ORDER BY date DESC, time DESC";

            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sessions.Add(new Session
                {
                    Id = Convert.ToInt32(reader["id"]),
                    PatientId = Convert.ToInt32(reader["patient_id"]),
                    Date = DateOnly.Parse(reader["date"].ToString()!),
                    Time = TimeOnly.Parse(reader["time"].ToString()!),
                    Description = reader["description"]?.ToString()
                });
            }

            return sessions;
        }

        public List<Apointment> GetAllAppointments()
        {
            List<Apointment> appointments = new();
            string query = "SELECT * FROM Appointments ORDER BY date DESC, time DESC";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                appointments.Add(new Apointment
                {
                    Id = Convert.ToInt32(reader["id"]),
                    PatientId = Convert.ToInt32(reader["patient_id"]),
                    Date = DateOnly.Parse(reader["date"].ToString()!),
                    Time = TimeOnly.Parse(reader["time"].ToString()!),
                    Note = reader["note"]?.ToString()
                });
            }
            return appointments;
        }

        public int GetTodaysAppointmentsCount()
        {
            List<Apointment> appointments = new();

            string query = "SELECT * FROM appointments WHERE date = @Today";

            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);

            string today = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            cmd.Parameters.AddWithValue("@Today", today);

            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                appointments.Add(new Apointment
                {
                    Id = Convert.ToInt32(reader["id"]),
                    PatientId = Convert.ToInt32(reader["patient_id"]),
                    Date = DateOnly.Parse(reader["date"].ToString()!),
                    Time = TimeOnly.Parse(reader["time"].ToString()!),
                    Note = reader["note"]?.ToString()
                });
            }
            return appointments.Count;
        }

        public DataTable GetAppointmentsByPatient(int PatientId)
        {
            string query = "SELECT * FROM Appointments WHERE Patient_i d = @PatientId";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", PatientId);
            using var adapter = new SQLiteDataAdapter(cmd);
            DataTable table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        public void AddAppointment(int patientId, DateTime date, TimeSpan time, string? note = null)
        {
            // Check for conflicts before adding
            if (HasAppointmentConflict(date, time, out var conflict))
            {
                var conflictTime = conflict!.Time.ToString("HH:mm");
                var blockEndTime = conflict.Time.AddMinutes(AppointmentDurationMinutes).ToString("HH:mm");
                throw new InvalidOperationException(
                    $"Cannot add appointment. There is already an appointment at {conflictTime} " +
                    $"which blocks the time slot until {blockEndTime}.");
            }

            string query = @"
                INSERT INTO Appointments (patient_id, date, time, note)
                VALUES (@PatientId, @Date, @Time, @Note)";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@Time", time.ToString(@"hh\:mm\:ss"));
            cmd.Parameters.AddWithValue("@Note", string.IsNullOrEmpty(note) ? DBNull.Value : note);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void DeleteAppointment(int appointmentId)
        {
            string query = "DELETE FROM Appointments WHERE id = @AppointmentId";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public Patient GetPatientByID(int patientId)
        {
            string query = "SELECT * FROM Patients WHERE id = @PatientId";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Patient
                {
                    Id = Convert.ToInt32(reader["id"]),
                    FirstName = reader["first_name"]?.ToString() ?? "",
                    LastName = reader["last_name"]?.ToString() ?? "",
                    Phone = reader["phone"]?.ToString(),
                    Email = reader["email"]?.ToString(),
                    Note = reader["note"]?.ToString()
                };
            }
            else
            {
                Console.WriteLine("Patient not found.");
                return null;
            }
        }
        public void AddSession(int patientId, DateTime date, TimeSpan time, string? description = null)
        {
            string query = @"
                INSERT INTO Visits (patient_id, date, time, description)
                VALUES (@PatientId, @Date, @Time, @Description)";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@Time", time.ToString(@"hh\:mm\:ss"));
            cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? DBNull.Value : description);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public List<MedicineInfo> GetAllMedicines()
        {
            var list = new List<MedicineInfo>();

            using var conn = new SQLiteConnection(_medicinesConnectionString);
            conn.Open();

            // Keep your medicines DB schema naming as-is
            const string query = @"
                SELECT NOM_DE_MARQUE AS name,
                       DOSAGE       AS dosage
                FROM products
                LIMIT 100;";

            using var cmd = new SQLiteCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var name = reader["name"]?.ToString() ?? string.Empty;
                var dosage = reader["dosage"]?.ToString() ?? string.Empty;

                list.Add(new MedicineInfo(name, dosage)
                {
                    Name = name,
                    Dosage = dosage
                });
            }

            return list;
        }

        public List<MedicineInfo> GetMedicinesByPrefix(string prefix)
        {
            var list = new List<MedicineInfo>();

            if (string.IsNullOrWhiteSpace(prefix))
                return list;

            using var conn = new SQLiteConnection(_medicinesConnectionString);
            conn.Open();

            // 1) Get up to 25 brand matches first
            const string brandQuery = @"
        SELECT 
            NOM_DE_MARQUE AS name,
            DOSAGE AS dosage
        FROM products
        WHERE NOM_DE_MARQUE LIKE @p || '%'
        LIMIT 25;";

            using (var cmd = new SQLiteCommand(brandQuery, conn))
            {
                cmd.Parameters.AddWithValue("@p", prefix);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var name = reader["name"]?.ToString() ?? string.Empty;
                    var dosage = reader["dosage"]?.ToString() ?? string.Empty;

                    list.Add(new MedicineInfo(name, dosage)
                    {
                        Name = name,
                        Dosage = dosage
                    });
                }
            }

            int remaining = 25 - list.Count;
            if (remaining <= 0)
                return list;

            const string dciQuery = @"
        SELECT 
            DENOMINATION_COMMUNE_INTERNATIONALE AS name,
            DOSAGE AS dosage
        FROM products
        WHERE DENOMINATION_COMMUNE_INTERNATIONALE LIKE @p || '%'
          AND DENOMINATION_COMMUNE_INTERNATIONALE NOT IN (
              SELECT NOM_DE_MARQUE
              FROM products
              WHERE NOM_DE_MARQUE LIKE @p || '%'
          )
        LIMIT @limit;";

            using (var cmd = new SQLiteCommand(dciQuery, conn))
            {
                cmd.Parameters.AddWithValue("@p", prefix);
                cmd.Parameters.AddWithValue("@limit", remaining);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var name = reader["name"]?.ToString() ?? string.Empty;
                    var dosage = reader["dosage"]?.ToString() ?? string.Empty;

                    list.Add(new MedicineInfo(name, dosage)
                    {
                        Name = name,
                        Dosage = dosage
                    });
                }
            }

            return list;
        }

        public Doctor GetDoctorCredentials()
        {
            const string query = @"
        SELECT
            first_name,
            last_name,
            specialty,
            email,
            phone,
            clinic_address,
            clinic_name,
            ordre,
            logo_path
        FROM UserCredentials
        WHERE id = 1
        LIMIT 1;";

            using var conn = new SQLiteConnection(_userCredentialsConnectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                throw new InvalidOperationException("Doctor information not found in the user credentials database. Please configure doctor credentials first.");

            var firstName = reader["first_name"]?.ToString() ?? string.Empty;
            var lastName = reader["last_name"]?.ToString() ?? string.Empty;
            var specialty = reader["specialty"]?.ToString() ?? string.Empty;
            var email = reader["email"]?.ToString() ?? string.Empty;
            var phoneNumber = reader["phone"]?.ToString() ?? string.Empty;
            var clinicAddress = reader["clinic_address"]?.ToString() ?? string.Empty;
            var clinicName = reader["clinic_name"]?.ToString() ?? string.Empty;
            var nDordre = reader["ordre"]?.ToString() ?? string.Empty;
            var logoPath = reader["logo_path"]?.ToString();

            return new Doctor(
                firstName,
                lastName,
                specialty,
                email,
                phoneNumber,
                clinicAddress,
                clinicName,
                nDordre,
                logoPath);
        }

        public List<Patient> GetPatientsByPrefix(string prefix, int limit = 25)
        {
            var patients = new List<Patient>();

            if (string.IsNullOrWhiteSpace(prefix))
                return patients;

            const string query = @"
        SELECT id, first_name, last_name, phone, email, note
        FROM Patients
        WHERE first_name LIKE @p || '%'
           OR last_name  LIKE @p || '%'
        ORDER BY last_name, first_name
        LIMIT @limit;";

            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@p", prefix);
            cmd.Parameters.AddWithValue("@limit", limit);

            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                patients.Add(new Patient
                {
                    Id = Convert.ToInt32(reader["id"]),
                    FirstName = reader["first_name"]?.ToString() ?? "",
                    LastName = reader["last_name"]?.ToString() ?? "",
                    Phone = reader["phone"]?.ToString(),
                    Email = reader["email"]?.ToString(),
                    Note = reader["note"]?.ToString()
                });
            }

            return patients;
        }

        public bool HasAppointmentConflict(DateTime date, TimeSpan time, out Apointment? conflictingAppointment)
        {
            conflictingAppointment = null;
            var proposedStart = date.Date + time;
            var proposedEnd = proposedStart.AddMinutes(AppointmentDurationMinutes);

            string query = "SELECT * FROM Appointments WHERE date = @Date";
            using var conn = new SQLiteConnection(_clinicConnectionString);
            using var cmd = new SQLiteCommand(query, conn);
            cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var existingTime = TimeOnly.Parse(reader["time"].ToString()!);
                var existingStart = date.Date + existingTime.ToTimeSpan();
                var existingEnd = existingStart.AddMinutes(AppointmentDurationMinutes);

                // Check for overlap: new appointment starts during existing, OR existing starts during new
                bool overlaps = (proposedStart < existingEnd && proposedEnd > existingStart);

                if (overlaps)
                {
                    conflictingAppointment = new Apointment
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        PatientId = Convert.ToInt32(reader["patient_id"]),
                        Date = DateOnly.Parse(reader["date"].ToString()!),
                        Time = existingTime,
                        Note = reader["note"]?.ToString()
                    };
                    return true;
                }
            }

            return false;
        }
    }
}
