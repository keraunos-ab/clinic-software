namespace clinicApp.Models
{
    public sealed class PatientDisplay
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = "";
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}