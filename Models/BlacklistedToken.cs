namespace ZUMI_Backend.Models;

public class BlacklistedToken
{
    public int Id { get; set; }
    public int TokenId { get; set; }
    public OutstandingToken Token { get; set; } // Navigation property
    public DateTime BlacklistedAt { get; set; }
}