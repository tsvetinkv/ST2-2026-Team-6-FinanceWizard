#nullable enable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Services.Builders;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Security.Claims;
using System.Text.Json;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

/// <summary>
/// AI assistant page: lets the user chat with a finance advisor (Google Gemini or the offline mock).
///
/// Design patterns used:
/// - Dependency Injection: IAiProvider (adapter over Gemini / mock) and ITransactionService are injected.
/// - Strategy pattern: IAiProvider can be swapped at startup (Gemini vs Mock) without changing this controller.
/// - Builder pattern: AiPromptBuilder assembles the LLM prompt step-by-step (system role → question → anonymised transactions).
/// - Session state: the chat history is kept in HttpContext.Session, not in the database.
/// </summary>
[Authorize]
public class AssistantController : Controller
{
    // Session key — one chat history per browser session.
    private const string SessionChatKey = "Advisor_Chat_History";

    private readonly IAiProvider _aiProvider;
    private readonly ITransactionService _transactionService;

    public AssistantController(IAiProvider aiProvider, ITransactionService transactionService)
    {
        _aiProvider = aiProvider;
        _transactionService = transactionService;
    }

    /// <summary>
    /// Extracts the current user's id from the auth cookie.
    /// Returns 0 when the claim is missing (should not happen for [Authorize] actions).
    /// </summary>
    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(claim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Loads the chat history from session and deserialises it.
    /// Returns an empty list if nothing has been saved yet.
    /// </summary>
    private List<ChatMessageDto> GetChatHistory()
    {
        var json = HttpContext.Session.GetString(SessionChatKey);
        return string.IsNullOrEmpty(json)
            ? new List<ChatMessageDto>()
            : JsonSerializer.Deserialize<List<ChatMessageDto>>(json) ?? new List<ChatMessageDto>();
    }

    /// <summary>
    /// Serialises the chat history back to session.
    /// </summary>
    private void SaveChatHistory(List<ChatMessageDto> history)
    {
        var json = JsonSerializer.Serialize(history);
        HttpContext.Session.SetString(SessionChatKey, json);
    }

    /// <summary>
    /// GET /Assistant — shows the chat page with the current history.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        var vm = new AssistantViewModel
        {
            ConversationHistory = GetChatHistory(),
            ActiveProvider = _aiProvider.ProviderName
        };
        return View(vm);
    }

    /// <summary>
    /// POST /Assistant — sends the user's question to the AI and appends both turns to the history.
    ///
    /// The prompt is built with the Builder pattern:
    /// 1. System role (tells the LLM it is a personal finance assistant).
    /// 2. The user's question.
    /// 3. Optional: the last 10 transactions (anonymised by the builder) for context.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AssistantViewModel model, CancellationToken cancellationToken)
    {
        var history = GetChatHistory();
        model.ConversationHistory = history;
        model.ActiveProvider = _aiProvider.ProviderName;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();

        // --- Builder pattern: assemble the prompt step by step. ---
        var builder = new AiPromptBuilder()
            .WithSystemRole("You are an expert personal finance assistant inside FinanceWizard.")
            .WithUserQuestion(model.UserQuestion);

        if (model.IncludeTransactions && userId > 0)
        {
            // Only the 10 most recent transactions are sent (to keep the prompt small and privacy-friendly).
            var allTransactions = await _transactionService.GetAllByUserIdAsync(userId);
            var recentTx = allTransactions.OrderByDescending(t => t.Date).Take(10);
            builder.WithAnonymizedTransactions(recentTx);
        }

        var prompt = builder.Build();

        history.Add(new ChatMessageDto { Sender = "User", Message = model.UserQuestion });

        // --- Strategy pattern: whichever IAiProvider is registered handles the call. ---
        var reply = await _aiProvider.GetResponseAsync(prompt, cancellationToken);
        history.Add(new ChatMessageDto { Sender = "AI", Message = reply });

        SaveChatHistory(history);

        // Reset the form so the user can type a new question without re-posting the old one.
        model.ConversationHistory = history;
        model.UserQuestion = string.Empty;
        ModelState.Clear();

        return View(model);
    }

    /// <summary>
    /// POST /Assistant/ClearHistory — wipes the chat history for this session.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ClearHistory()
    {
        HttpContext.Session.Remove(SessionChatKey);
        return RedirectToAction(nameof(Index));
    }
}
