
namespace clinicApp.Models
{
    public class Apointment
    {
        public required int Id { get; set; }
        public required int PatientId { get; set; }
        public required DateOnly Date { get; set; }
        public required TimeOnly Time { get; set; }
        public string? Note { get; set; }
    }
}
