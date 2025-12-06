namespace ZUMI_Backend.Models.DTOs
{
    public class PersonRoleDto
    {
        public Guid PersonId { get; set; }
        public bool IsLiked { get; set; } = false;
        public bool IsOwner { get; set; } = false;
        public bool IsParticipating { get; set; } = false;
        
        //public PersonDto Person { get; set; } = null!;
        
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        
        public string Email { get; set; }
    }
}