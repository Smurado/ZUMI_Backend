namespace ZUMI_Backend.Models.DTOs;

public record FeedbackDto
{
    // Parameterloser Konstruktor (wichtig für AutoMapper + Vererbung)
    public FeedbackDto() { }

    public FeedbackDto(
        Guid id,
        string category,
        string affectedComponent,
        string subject,
        string message,
        int? rating,
        DateTimeOffset createdAt,
        string senderName,
        Guid senderId)
    {
        Id = id;
        Category = category;
        AffectedComponent = affectedComponent;
        Subject = subject;
        Message = message;
        CreatedAt = createdAt;
        SenderName = senderName;
        SenderId = senderId;
    }

    public Guid Id { get; init; }
    public string Category { get; init; } = null!;
    public string AffectedComponent { get; init; } = null!;
    public string Subject { get; init; } = null!;
    public string Message { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public string SenderName { get; init; } = null!;
    public Guid SenderId { get; init; }
}