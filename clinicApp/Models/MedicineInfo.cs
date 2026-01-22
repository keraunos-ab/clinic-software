namespace clinicApp.Models
{
    public class MedicineInfo
    {
        public required string Name { get; set; }
        public required string Dosage { get; set; }
        public string FullDisplay => $"{Name} ({Dosage})";
        public override string ToString() => FullDisplay;
        public MedicineInfo(string name, string dosage)
        {
            this.Name = name;
            this.Dosage = dosage;
        }
    }
}
