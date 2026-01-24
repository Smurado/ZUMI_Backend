namespace ZUMI_Backend.Models.DTOs
{
    using Enums;
    public class PersonDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Plz { get; set; }
        public string Sprache { get; set; }
        
        public Altersgruppe Altersgruppe { get; set; }

        public List<ProjectRoleDto> Projekte { get; set; } = new();
    }
}