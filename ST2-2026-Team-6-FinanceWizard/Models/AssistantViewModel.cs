#nullable enable
using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models;

public sealed class AssistantViewModel
{
    [Required(ErrorMessage = "Please, enter your question.")]
    [StringLength(500, ErrorMessage = "The question cannot exceed 500 characters.")]
    public string UserQuestion { get; set; } = string.Empty;

    public bool IncludeTransactions { get; set; } = true;

    public string? ActiveProvider { get; set; }

    public List<ChatMessageDto> ConversationHistory { get; set; } = new();

    public string? ErrorMessage { get; set; }
}

public sealed class ChatMessageDto
{
    public string Sender { get; set; } = string.Empty; // "User" or "AI"
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}