namespace ZUMI_Backend.Models.DTOs
{
    using Enums;

    public class UpdatePersonDto
    {
        public string? Email { get; set; }  // Optional, für Partial-Update

        public string? Password { get; set; }  // Plaintext; wird im Handler gehasht, falls angegeben

        public string? Plz { get; set; }

        public string? Land { get; set; }
        
        public string? Ort { get; set; }
        
        public string? Sprache { get; set; }

        public string? Interessen { get; set; }

        public string? Staerken { get; set; }

        public string? Avatar { get; set; }  // URL/Path zu Bild

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public Altersgruppe Altersgruppe { get; set; }  // Enum, nullable für Partial
    }
}