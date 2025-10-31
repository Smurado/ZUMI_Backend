namespace ZUMI_Backend.Models.DTOs
{
    public class PersonDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Plz { get; set; }
        public string Sprache { get; set; }
        
        public Guid RolleId { get; set; }
        // Navigation: Flach halten, z. B. nur IDs oder einfache DTOs
        public List<Guid> ProjekteIds { get; set; } = new List<Guid>();
    }
}