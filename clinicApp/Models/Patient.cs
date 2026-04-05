namespace clinicApp.Models
{
    public class Patient
    {
        public required int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required DateTime DateOfBirth { get; set; }
        public required string Gender { get; set; }
        public double? weight { get; set; }
        public required string BloodType { get; set; }
        public required string Phone { get; set; }
        public string? Email { get; set; }
        public string? Note { get; set; }
    }
}
