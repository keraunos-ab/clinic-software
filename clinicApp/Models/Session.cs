using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace clinicApp.Models
{
    public class Session
    {
        public required int Id { get; set; }
        public required int PatientId { get; set; }
        public required DateOnly Date { get; set; }
        public required TimeOnly Time { get; set; }
        public string? Description { get; set; }
    }
}
