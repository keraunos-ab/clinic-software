
namespace clinicApp.Models
{
    public class Checkup
    {
        public required int Id { get; set; }
        public required int PatientId { get; set; }
        public required int ConsultationId { get; set; }
        public required DateOnly Date { get; set; }
        public required TimeOnly Time { get; set; }
        public string? Description { get; set; }
    }
}