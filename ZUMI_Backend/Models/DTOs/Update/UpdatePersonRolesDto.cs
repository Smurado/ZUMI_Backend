namespace ZUMI_Backend.Models.DTOs
{
    public class UpdatePersonRolesDto
    {
        public List<PersonUpdateItemDto> Personen { get; set; }
    }

    public class PersonUpdateItemDto
    {
        public Guid PersonId { get; set; }
        public bool IsOwner { get; set; }
        public bool RemoveFromProject { get; set; }
        
        // NEU: Liste der Rollen-IDs, die der User haben soll
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }
}