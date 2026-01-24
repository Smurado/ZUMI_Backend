namespace ZUMI_Backend.Models.DTOs;



public record FeedbackDetailDto : FeedbackDto
{
    public FeedbackDetailDto() { } // wieder parameterlos

    public string? RecipientName { get; init; }
    public Guid? RecipientId { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public string? SenderEmail { get; init; }
}