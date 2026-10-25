using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models;

public sealed class AssistantViewModel
{
    [Required(ErrorMessage = "Моля, въведете вашия въпрос.")]
    [StringLength(500, ErrorMessage = "Въпросът не може да надвишава 500 символа.")]
    public string UserQuestion { get; set; } = string.Empty;

    public bool IncludeTransactions { get; set; } = true;

    public string? ActiveProvider { get; set; }

    public List<ChatMessageDto> ConversationHistory { get; set; } = new();
}

public sealed class ChatMessageDto
{
    public string Sender { get; set; } = string.Empty; // "User" или "AI"
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}