namespace ZUMI_Backend.Models.DTOs;

using Enums;

public record CreateFeedbackDto(
    FeedbackCategory Category,
    FeedbackAffectedComponent AffectedComponent,
    string Subject,
    string Message,
    int? Rating = null,                 // optional
    Guid? RecipientId = null            //
);