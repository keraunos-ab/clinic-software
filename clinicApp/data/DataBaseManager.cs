using clinicApp.Models;
using Microsoft.Data.Sqlite;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace clinicApp.data
{
    internal class DataBaseManager
    {
        private readonly string _clinicConnectionString;
        private readonly string _medicinesConnectionString;

        public static string DefaultConnectionString { get; } =
            "Host=localhost;Port=5432;Database=clinicDatabase;Username=postgres;Password=blaze123";

        public int AppointmentDurationMinutes { get; set; } = 30;

        public DataBaseManager(
            string? clinicConnectionString = null,
            string medicinesDbPath = "data\\medicines.db")
        {
            _clinicConnectionString = clinicConnectionString ?? DefaultConnectionString;

            if (string.IsNullOrWhiteSpace(medicinesDbPath))
                medicinesDbPath = "data\\medicines.db";

            if (!Path.IsPathRooted(medicinesDbPath))
                medicinesDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, medicinesDbPath);

            _medicinesConnectionString = $"Data Source={medicinesDbPath}";
        }

        public void InitializeDatabase()
        {
            // Ensure the PostgreSQL database exists
            var builder = new NpgsqlConnectionStringBuilder(_clinicConnectionString);
            var dbName = builder.Database!;
            builder.Database = "postgres";

            using (var adminConn = new NpgsqlConnection(builder.ToString()))
            {
                adminConn.Open();
                using var checkCmd = new NpgsqlCommand(
                    "SELECT 1 FROM pg_database WHERE datname = @d", adminConn);
                checkCmd.Parameters.AddWithValue("@d", dbName);

                if (checkCmd.ExecuteScalar() == null)
                {
                    using var createCmd = new NpgsqlCommand(
                        $"CREATE DATABASE \"{dbName}\"", adminConn);
                    createCmd.ExecuteNonQuery();
                }
            }

            using var conn = new NpgsqlConnection(_clinicConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("", conn);

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Patients (
                    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    first_name TEXT NOT NULL,
                    last_name TEXT NOT NULL,
                    date_of_birth TEXT NOT NULL,
                    gender TEXT NOT NULL,
                    weight TEXT,
                    blood_type TEXT,
                    phone TEXT NOT NULL,
                    email TEXT,
                    note TEXT
                );";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Appointments (
                    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    patient_id INTEGER NOT NULL REFERENCES Patients(id),
                    date TEXT NOT NULL,
                    time TEXT NOT NULL,
                    note TEXT
                );";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Visits (
                    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    patient_id INTEGER NOT NULL REFERENCES Patients(id),
                    consultation_id INTEGER NOT NULL REFERENCES Consultations(id),
                    date TEXT NOT NULL,
                    time TEXT NOT NULL,
                    description TEXT
                );";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Consultations (
                    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    patient_id INTEGER NOT NULL REFERENCES Patients(id),
                    date TEXT NOT NULL,
                    motiv TEXT,
                    bilan_image BYTEA,
                    antecedents TEXT,
                    medications TEXT,
                    hdm TEXT,
                    etat_clinique TEXT,
                    cat TEXT
                );";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS UserCredentials (
                    id INTEGER PRIMARY KEY CHECK (id = 1),
                    first_name TEXT,
                    last_name TEXT,
                    specialty TEXT,
                    email TEXT,
                    phone TEXT,
                    clinic_address TEXT,
                    clinic_name TEXT,
                    ordre TEXT,
                    logo_path TEXT,
                    password_hash TEXT
                );";
            cmd.ExecuteNonQuery();

            // Migrate legacy numeric gender values to strings
            cmd.CommandText = "UPDATE Patients SET gender = 'Male' WHERE gender = '0';";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "UPDATE Patients SET gender = 'Female' WHERE gender = '1';";
            cmd.ExecuteNonQuery();
        }

        public List<Patient> GetAllPatients()
        {
            var patients = new List<Patient>();

            using var conn = new NpgsqlConnection(_clinicConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM Patients ORDER BY LOWER(last_name), LOWER(first_name)", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                patients.Add(new Patient
                {
                    Id = Convert.ToInt32(reader["id"]),
                    FirstName = reader["first_name"].ToString() ?? "",
                    LastName = reader["last_name"].ToString() ?? "",
                    DateOfBirth = DateTime.Parse(reader["date_of_birth"].ToString()!),
                    Gender = reader["gender"].ToString()!,
                    weight = reader["weight"] is DBNull ? null : double.Parse(CryptoHelper.SafeDecrypt(reader["weight"].ToString())),
                    BloodType = reader["blood_type"] is DBNull ? "" : CryptoHelper.SafeDecrypt(reader["blood_type"].ToString()!),
                    Phone = CryptoHelper.SafeDecrypt(reader["phone"].ToString()!),
                    Email = reader["email"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["email"].ToString()),
                    Note = reader["note"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["note"].ToString())
                });
            }

            return patients;
        }



        public int GetPatientIdByName(string firstName, string lastName)
        {
            string query = "SELECT * FROM Patients WHERE first_name = @FirstName AND last_name = @LastName";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
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

        public void AddPatient(string firstName, string lastName, string phone, string email, string gender, DateTime dateOfBirth, string? note = null, double? weight = null, string? bloodType = null)
        {
            string checkQuery = "SELECT COUNT(*) FROM Patients WHERE first_name = @FirstName AND last_name = @LastName";
            using (var conn = new NpgsqlConnection(_clinicConnectionString))
            using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@FirstName", firstName);
                checkCmd.Parameters.AddWithValue("@LastName", lastName);
                conn.Open();
                long count = (long)checkCmd.ExecuteScalar()!;
                if (count > 0)
                {
                    throw new Exception("A patient with this first and last name already exists.");
                }
                conn.Close();
            }

            string insertQuery = @"
        INSERT INTO Patients (first_name, last_name, phone, email, note, date_of_birth, gender, weight, blood_type)
        VALUES (@FirstName, @LastName, @Phone, @Email, @Note, @DateOfBirth, @Gender, @Weight, @BloodType)";
            using (var conn = new NpgsqlConnection(_clinicConnectionString))
            using (var cmd = new NpgsqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@Phone", CryptoHelper.Encrypt(phone));
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? DBNull.Value : (object)CryptoHelper.Encrypt(email));
                cmd.Parameters.AddWithValue("@Note", string.IsNullOrEmpty(note) ? DBNull.Value : (object)CryptoHelper.Encrypt(note));
                cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@Gender", gender);
                cmd.Parameters.AddWithValue("@Weight", weight.HasValue ? (object)CryptoHelper.Encrypt(weight.Value.ToString()) : DBNull.Value);
                cmd.Parameters.AddWithValue("@BloodType", string.IsNullOrEmpty(bloodType) ? DBNull.Value : (object)CryptoHelper.Encrypt(bloodType));
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }


        public void RemovePatient(int patientId)
        {
            string query = "DELETE FROM Patients WHERE id = @PatientId";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void UpdatePatientByID(int patientId, string firstName, string lastName, string phone, string email, string gender, DateTime dateOfBirth, string? note = null, double? weight = null, string? bloodType = null)
        {
            string query = @"
                UPDATE Patients
                SET first_name = @FirstName,
                    last_name = @LastName,
                    phone = @Phone,
                    gender = @Gender,
                    weight = @Weight,
                    blood_type = @BloodType,
                    date_of_birth = @DateOfBirth,
                    email = @Email,
                    note = @Note
                WHERE id = @PatientId";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@Phone", CryptoHelper.Encrypt(phone));
            cmd.Parameters.AddWithValue("@Gender", gender);
            cmd.Parameters.AddWithValue("@Weight", weight.HasValue ? (object)CryptoHelper.Encrypt(weight.Value.ToString()) : DBNull.Value);
            cmd.Parameters.AddWithValue("@BloodType", string.IsNullOrEmpty(bloodType) ? DBNull.Value : (object)CryptoHelper.Encrypt(bloodType));
            cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? DBNull.Value : (object)CryptoHelper.Encrypt(email));
            cmd.Parameters.AddWithValue("@Note", string.IsNullOrEmpty(note) ? DBNull.Value : (object)CryptoHelper.Encrypt(note));
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public int GetTodaysSessionCount()
        {
            List<Checkup> sessions = new();
            string query = "SELECT * FROM Visits WHERE date = @Today";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            string today = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            cmd.Parameters.AddWithValue("@Today", today);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sessions.Add(new Checkup
                {
                    Id = Convert.ToInt32(reader["id"]),
                    PatientId = Convert.ToInt32(reader["patient_id"]),
                    ConsultationId = Convert.ToInt32(reader["consultation_id"]),
                    Date = DateOnly.Parse(reader["date"].ToString()!),
                    Time = TimeOnly.Parse(reader["time"].ToString()!),
                    Description = reader["description"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["description"]?.ToString())
                });
            }
            return sessions.Count;
        }

        public List<Checkup> GetSessionsByPatient(int patientId)
        {
            List<Checkup> sessions = new();
            string query = "SELECT * FROM Visits WHERE patient_id = @PatientId ORDER BY date DESC, time DESC";

            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sessions.Add(new Checkup
                {
                    Id = Convert.ToInt32(reader["id"]),
                    PatientId = Convert.ToInt32(reader["patient_id"]),
                    ConsultationId = Convert.ToInt32(reader["consultation_id"]),
                    Date = DateOnly.Parse(reader["date"].ToString()!),
                    Time = TimeOnly.Parse(reader["time"].ToString()!),
                    Description = reader["description"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["description"]?.ToString())
                });
            }

            return sessions;
        }

        public List<Checkup> GetSessionsByConsultation(int consultationId)
        {
            List<Checkup> sessions = new();
            string query = "SELECT * FROM Visits WHERE consultation_id = @ConsultationId ORDER BY date DESC, time DESC";

            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ConsultationId", consultationId);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sessions.Add(new Checkup
                {
                    Id = Convert.ToInt32(reader["id"]),
                    PatientId = Convert.ToInt32(reader["patient_id"]),
                    ConsultationId = Convert.ToInt32(reader["consultation_id"]),
                    Date = DateOnly.Parse(reader["date"].ToString()!),
                    Time = TimeOnly.Parse(reader["time"].ToString()!),
                    Description = reader["description"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["description"]?.ToString())
                });
            }

            return sessions;
        }

        public List<Apointment> GetAllAppointments()
        {
            List<Apointment> appointments = new();
            string query = "SELECT * FROM Appointments ORDER BY date DESC, time DESC";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
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
                    Note = reader["note"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["note"]?.ToString())
                });
            }
            return appointments;
        }

        public int GetTodaysAppointmentsCount()
        {
            List<Apointment> appointments = new();

            string query = "SELECT * FROM Appointments WHERE date = @Today";

            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);

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
                    Note = reader["note"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["note"]?.ToString())
                });
            }
            return appointments.Count;
        }

        public DataTable GetAppointmentsByPatient(int PatientId)
        {
            string query = "SELECT * FROM Appointments WHERE patient_id = @PatientId";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", PatientId);
            using var reader = cmd.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);

            // Decrypt encrypted columns
            if (table.Columns.Contains("note"))
            {
                foreach (DataRow row in table.Rows)
                {
                    if (row["note"] is not DBNull)
                        row["note"] = CryptoHelper.SafeDecrypt(row["note"].ToString());
                }
                table.AcceptChanges();
            }

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
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@Time", time.ToString(@"hh\:mm\:ss"));
            cmd.Parameters.AddWithValue("@Note", string.IsNullOrEmpty(note) ? DBNull.Value : (object)CryptoHelper.Encrypt(note));
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void DeleteAppointment(int appointmentId)
        {
            string query = "DELETE FROM Appointments WHERE id = @AppointmentId";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public Patient? GetPatientByID(int patientId)
        {
            string query = "SELECT * FROM Patients WHERE id = @PatientId";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Patient
                {
                    Id = Convert.ToInt32(reader["id"]),
                    FirstName = reader["first_name"].ToString() ?? "",
                    LastName = reader["last_name"].ToString() ?? "",
                    DateOfBirth = DateTime.Parse(reader["date_of_birth"].ToString()!),
                    Gender = reader["gender"].ToString()!,
                    weight = reader["weight"] is DBNull ? null : double.Parse(CryptoHelper.SafeDecrypt(reader["weight"].ToString())),
                    BloodType = reader["blood_type"] is DBNull ? "" : CryptoHelper.SafeDecrypt(reader["blood_type"].ToString()!),
                    Phone = CryptoHelper.SafeDecrypt(reader["phone"].ToString()!),
                    Email = reader["email"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["email"].ToString()),
                    Note = reader["note"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["note"].ToString())
                };
            }
            else
            {
                Console.WriteLine("Patient not found.");
                return null;
            }
        }

        public void deletePatient(string firstName, string lastName)
        {
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            conn.Open();

            string checkQuery = "SELECT id FROM Patients WHERE first_name = @FirstName AND last_name = @LastName";
            using var checkCmd = new NpgsqlCommand(checkQuery, conn);
            checkCmd.Parameters.AddWithValue("@FirstName", firstName);
            checkCmd.Parameters.AddWithValue("@LastName", lastName);

            var result = checkCmd.ExecuteScalar();
            if (result == null)
            {
                Console.WriteLine("Patient not found.");
                return;
            }

            int patientId = Convert.ToInt32(result);

            string deleteConsultations = "DELETE FROM Consultations WHERE patient_id = @PatientId";
            using var cmd0 = new NpgsqlCommand(deleteConsultations, conn);
            cmd0.Parameters.AddWithValue("@PatientId", patientId);
            cmd0.ExecuteNonQuery();

            string deleteAppointments = "DELETE FROM Appointments WHERE patient_id = @PatientId";
            using var cmd1 = new NpgsqlCommand(deleteAppointments, conn);
            cmd1.Parameters.AddWithValue("@PatientId", patientId);
            cmd1.ExecuteNonQuery();

            string deleteVisits = "DELETE FROM Visits WHERE patient_id = @PatientId";
            using var cmd2 = new NpgsqlCommand(deleteVisits, conn);
            cmd2.Parameters.AddWithValue("@PatientId", patientId);
            cmd2.ExecuteNonQuery();

            // Finally, delete the patient
            string deletePatient = "DELETE FROM Patients WHERE id = @PatientId";
            using var cmd3 = new NpgsqlCommand(deletePatient, conn);
            cmd3.Parameters.AddWithValue("@PatientId", patientId);
            cmd3.ExecuteNonQuery();
        }
        public void AddSession(int patientId, int consultationId, DateTime date, TimeSpan time, string? description = null)
        {
            string query = @"
                INSERT INTO Visits (patient_id, consultation_id, date, time, description)
                VALUES (@PatientId, @ConsultationId, @Date, @Time, @Description)";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            cmd.Parameters.AddWithValue("@ConsultationId", consultationId);
            cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@Time", time.ToString(@"hh\:mm\:ss"));
            cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? DBNull.Value : (object)CryptoHelper.Encrypt(description));
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void AddConsultation(int patientId, string date, string? motiv, byte[]? bilanImage,
            string[]? antecedents, string[]? medications, string? hdm, string? etatClinique, string? cat)
        {
            string query = @"
                INSERT INTO Consultations (patient_id, date, motiv, bilan_image, antecedents, medications, hdm, etat_clinique, cat)
                VALUES (@PatientId, @Date, @Motiv, @BilanImage, @Antecedents, @Medications, @Hdm, @EtatClinique, @Cat)";

            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            cmd.Parameters.AddWithValue("@Date", date);
            cmd.Parameters.AddWithValue("@Motiv", string.IsNullOrEmpty(motiv) ? DBNull.Value : (object)CryptoHelper.Encrypt(motiv));

            if (bilanImage != null)
                cmd.Parameters.AddWithValue("@BilanImage", CryptoHelper.EncryptBytes(bilanImage));
            else
                cmd.Parameters.Add(new NpgsqlParameter("@BilanImage", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = DBNull.Value });

            cmd.Parameters.AddWithValue("@Antecedents",
                antecedents != null && antecedents.Length > 0
                    ? (object)CryptoHelper.Encrypt(JsonSerializer.Serialize(antecedents))
                    : DBNull.Value);
            cmd.Parameters.AddWithValue("@Medications",
                medications != null && medications.Length > 0
                    ? (object)CryptoHelper.Encrypt(JsonSerializer.Serialize(medications))
                    : DBNull.Value);
            cmd.Parameters.AddWithValue("@Hdm", string.IsNullOrEmpty(hdm) ? DBNull.Value : (object)CryptoHelper.Encrypt(hdm));
            cmd.Parameters.AddWithValue("@EtatClinique", string.IsNullOrEmpty(etatClinique) ? DBNull.Value : (object)CryptoHelper.Encrypt(etatClinique));
            cmd.Parameters.AddWithValue("@Cat", string.IsNullOrEmpty(cat) ? DBNull.Value : (object)CryptoHelper.Encrypt(cat));

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public List<Consultation> GetConsultationsByPatient(int patientId)
        {
            var list = new List<Consultation>();
            string query = "SELECT * FROM Consultations WHERE patient_id = @PatientId ORDER BY date DESC, id DESC";

            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var antJson = reader["antecedents"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["antecedents"].ToString());
                var medJson = reader["medications"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["medications"].ToString());

                list.Add(new Consultation
                {
                    Id = Convert.ToInt32(reader["id"]),
                    PatientId = Convert.ToInt32(reader["patient_id"]),
                    Date = reader["date"].ToString() ?? "",
                    Motiv = reader["motiv"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["motiv"].ToString()),
                    BilanImage = reader["bilan_image"] is DBNull ? null : CryptoHelper.SafeDecryptBytes((byte[])reader["bilan_image"]),
                    Antecedents = string.IsNullOrEmpty(antJson) ? null : JsonSerializer.Deserialize<string[]>(antJson),
                    Medications = string.IsNullOrEmpty(medJson) ? null : JsonSerializer.Deserialize<string[]>(medJson),
                    Hdm = reader["hdm"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["hdm"].ToString()),
                    EtatClinique = reader["etat_clinique"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["etat_clinique"].ToString()),
                    Cat = reader["cat"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["cat"].ToString())
                });
            }

            return list;
        }

        public void DeleteConsultation(int consultationId)
        {
            string query = "DELETE FROM Consultations WHERE id = @Id";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", consultationId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public List<MedicineInfo> GetAllMedicines()
        {
            var list = new List<MedicineInfo>();

            using var conn = new SqliteConnection(_medicinesConnectionString);
            conn.Open();

            // Keep your medicines DB schema naming as-is
            const string query = @"
                SELECT NOM_DE_MARQUE AS name,
                       DOSAGE       AS dosage
                FROM products
                LIMIT 100;";

            using var cmd = new SqliteCommand(query, conn);
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

            using var conn = new SqliteConnection(_medicinesConnectionString);
            conn.Open();

            // 1) Get up to 25 brand matches first
            const string brandQuery = @"
        SELECT 
            NOM_DE_MARQUE AS name,
            DOSAGE AS dosage
        FROM products
        WHERE NOM_DE_MARQUE LIKE @p || '%'
        LIMIT 25;";

            using (var cmd = new SqliteCommand(brandQuery, conn))
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

            using (var cmd = new SqliteCommand(dciQuery, conn))
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

            using var conn = new NpgsqlConnection(_clinicConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                throw new InvalidOperationException("Doctor information not found in the user credentials database. Please configure doctor credentials first.");

            var firstName = reader["first_name"] is DBNull ? string.Empty : CryptoHelper.SafeDecrypt(reader["first_name"].ToString());
            var lastName = reader["last_name"] is DBNull ? string.Empty : CryptoHelper.SafeDecrypt(reader["last_name"].ToString());
            var specialty = reader["specialty"] is DBNull ? string.Empty : CryptoHelper.SafeDecrypt(reader["specialty"].ToString());
            var email = reader["email"] is DBNull ? string.Empty : CryptoHelper.SafeDecrypt(reader["email"].ToString());
            var phoneNumber = reader["phone"] is DBNull ? string.Empty : CryptoHelper.SafeDecrypt(reader["phone"].ToString());
            var clinicAddress = reader["clinic_address"] is DBNull ? string.Empty : CryptoHelper.SafeDecrypt(reader["clinic_address"].ToString());
            var clinicName = reader["clinic_name"] is DBNull ? string.Empty : CryptoHelper.SafeDecrypt(reader["clinic_name"].ToString());
            var nDordre = reader["ordre"] is DBNull ? string.Empty : CryptoHelper.SafeDecrypt(reader["ordre"].ToString());
            var logoPath = reader["logo_path"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["logo_path"].ToString());

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
                SELECT id, first_name, last_name, date_of_birth, gender, weight, blood_type, phone, email, note
                FROM Patients
                WHERE first_name ILIKE @p || '%'
                   OR last_name  ILIKE @p || '%'
                ORDER BY last_name, first_name
                LIMIT @limit;";

            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@p", prefix);
            cmd.Parameters.AddWithValue("@limit", limit);

            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                patients.Add(new Patient
                {
                    Id = Convert.ToInt32(reader["id"]),
                    FirstName = reader["first_name"].ToString() ?? "",
                    LastName = reader["last_name"].ToString() ?? "",
                    DateOfBirth = DateTime.Parse(reader["date_of_birth"].ToString()!),
                    Gender = reader["gender"].ToString()!,
                    weight = reader["weight"] is DBNull ? null : double.Parse(CryptoHelper.SafeDecrypt(reader["weight"].ToString())),
                    BloodType = reader["blood_type"] is DBNull ? "" : CryptoHelper.SafeDecrypt(reader["blood_type"].ToString()!),
                    Phone = CryptoHelper.SafeDecrypt(reader["phone"].ToString()!),
                    Email = reader["email"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["email"].ToString()),
                    Note = reader["note"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["note"].ToString())
                });
            }

            return patients;
        }

        public List<Patient> GetPatientsByBirthdate(string birthdateQuery, int limit = 25)
        {
            var patients = new List<Patient>();

            if (string.IsNullOrWhiteSpace(birthdateQuery))
                return patients;

            string datePattern = ConvertUserInputToDbPattern(birthdateQuery);

            const string query = @"
                SELECT id, first_name, last_name, date_of_birth, gender, weight, blood_type, phone, email, note
                FROM Patients
                WHERE date_of_birth LIKE @datePattern
                ORDER BY date_of_birth, last_name, first_name
                LIMIT @limit;";

            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@datePattern", datePattern);
            cmd.Parameters.AddWithValue("@limit", limit);

            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                patients.Add(new Patient
                {
                    Id = Convert.ToInt32(reader["id"]),
                    FirstName = reader["first_name"].ToString() ?? "",
                    LastName = reader["last_name"].ToString() ?? "",
                    DateOfBirth = DateTime.Parse(reader["date_of_birth"].ToString()!),
                    Gender = reader["gender"].ToString()!,
                    weight = reader["weight"] is DBNull ? null : double.Parse(CryptoHelper.SafeDecrypt(reader["weight"].ToString())),
                    BloodType = reader["blood_type"] is DBNull ? "" : CryptoHelper.SafeDecrypt(reader["blood_type"].ToString()!),
                    Phone = CryptoHelper.SafeDecrypt(reader["phone"].ToString()!),
                    Email = reader["email"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["email"].ToString()),
                    Note = reader["note"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["note"].ToString())
                });
            }

            return patients;
        }

        private string ConvertUserInputToDbPattern(string userInput)
        {
            var parts = userInput.Split('/');

            if (parts.Length == 1 && !string.IsNullOrEmpty(parts[0]))
            {
                return $"%-{parts[0].PadLeft(2, '0')}";
            }

            if (parts.Length == 2)
            {
                string day = parts[0].PadLeft(2, '0');
                if (string.IsNullOrEmpty(parts[1]))
                {
                    return $"%-{day}";
                }
                string month = parts[1].PadLeft(2, '0');
                return $"%-{month}-{day}";
            }

            if (parts.Length == 3)
            {
                string day = parts[0].PadLeft(2, '0');
                string month = parts[1].PadLeft(2, '0');
                if (string.IsNullOrEmpty(parts[2]))
                {
                    return $"%-{month}-{day}";
                }
                string year = parts[2];
                if (year.Length < 4)
                {
                    return $"%{year}%-{month}-{day}";
                }
                return $"{year}-{month}-{day}";
            }

            return $"%{userInput}%";
        }

        public bool HasAppointmentConflict(DateTime date, TimeSpan time, out Apointment? conflictingAppointment)
        {
            conflictingAppointment = null;
            var proposedStart = date.Date + time;
            var proposedEnd = proposedStart.AddMinutes(AppointmentDurationMinutes);

            string query = "SELECT * FROM Appointments WHERE date = @Date";
            using var conn = new NpgsqlConnection(_clinicConnectionString);
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var existingTime = TimeOnly.Parse(reader["time"].ToString()!);
                var existingStart = date.Date + existingTime.ToTimeSpan();
                var existingEnd = existingStart.AddMinutes(AppointmentDurationMinutes);

                if (proposedStart < existingEnd && proposedEnd > existingStart)
                {
                    conflictingAppointment = new Apointment
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        PatientId = Convert.ToInt32(reader["patient_id"]),
                        Date = DateOnly.Parse(reader["date"].ToString()!),
                        Time = existingTime,
                        Note = reader["note"] is DBNull ? null : CryptoHelper.SafeDecryptOrNull(reader["note"]?.ToString())
                    };
                    return true;
                }
            }

            return false;
        }
    }
}
