namespace clinicApp.Models
{
    public class Consultation
    {
        public required int Id { get; set; }
        public required int PatientId { get; set; }
        public required string Date { get; set; }
        public string? Motiv { get; set; }
        public byte[]? BilanImage { get; set; }
        public string[]? Antecedents { get; set; }
        public string[]? Medications { get; set; }
        public string? Hdm { get; set; }
        public string? EtatClinique { get; set; }
        public string? Cat { get; set; }
    }
}
