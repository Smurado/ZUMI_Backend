namespace ZUMI_Backend.Models;
using Enums;

public class Feedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public Person User { get; set; } = null!;

    // Optional: An wen geht das Feedback (z. B. Support-Team, Admin, oder eine andere Person)
    public Person Recipient { get; set; }
    
    public FeedbackCategory Category { get; set; } = FeedbackCategory.General;
    
    public FeedbackAffectedComponent AffectedComponent { get; set; } = FeedbackAffectedComponent.Sonstiges;

    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;
    public bool IsResolved { get; set; } = false;
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? AdminComment { get; set; }
}