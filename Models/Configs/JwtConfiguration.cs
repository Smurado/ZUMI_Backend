namespace ZUMI_Backend.Models.Configs;

public class JwtConfiguration
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpireDays { get; set; }
}