namespace ZUMI_Backend.Models;

public class OutstandingToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public Person User { get; set; } // Navigation property
    public string Jti { get; set; }
}