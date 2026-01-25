using System;

namespace clinicApp.Models
{
    internal class Doctor
    {
        String FirstName { get; set; }
        String LastName { get; set; }
        String Specialty { get; set; }
        String Email { get; set; }
        String PhoneNumber { get; set; }
        String ClinicAddress { get; set; }
        String ClinicName { get; set; }
        String N_dordre { get; set; }
        String? LogoPath { get; set; }

        public Doctor(String FirstName, String LastName, String Specialty, String Email, String PhoneNumber, String ClinicAddress, String ClinicName, String N_dordre, String? LogoPath)
        {
            this.FirstName = FirstName;
            this.LastName = LastName;
            this.Specialty = Specialty;
            this.Email = Email;
            this.PhoneNumber = PhoneNumber;
            this.ClinicAddress = ClinicAddress;
            this.ClinicName = ClinicName;
            this.N_dordre = N_dordre;
            this.LogoPath = LogoPath;
        }

        // getters
        public String getFirstName() { return FirstName; }
        public String getLastName() { return LastName; }
        public String getSpecialty() { return Specialty; }
        public String getEmail() { return Email; }
        public String getPhoneNumber() { return PhoneNumber; }
        public String getClinicAddress() { return ClinicAddress; }
        public String getClinicName() { return ClinicName; }
        public String getN_dordre() { return N_dordre; }
        public String? getLogoPath() { return LogoPath; }
    }
}