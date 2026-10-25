#nullable enable
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Services.Builders;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

[Authorize]
public class AssistantController : Controller
{
    private const string SessionChatKey = "Advisor_Chat_History";
    private readonly IAiProvider _aiProvider;
    private readonly ITransactionService _transactionService;

    public AssistantController(IAiProvider aiProvider, ITransactionService transactionService)
    {
        _aiProvider = aiProvider;
        _transactionService = transactionService;
    }

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(claim, out var userId) ? userId : 0;
    }

    private List<ChatMessageDto> GetChatHistory()
    {
        var json = HttpContext.Session.GetString(SessionChatKey);
        return string.IsNullOrEmpty(json)
            ? new List<ChatMessageDto>()
            : JsonSerializer.Deserialize<List<ChatMessageDto>>(json) ?? new List<ChatMessageDto>();
    }

    private void SaveChatHistory(List<ChatMessageDto> history)
    {
        var json = JsonSerializer.Serialize(history);
        HttpContext.Session.SetString(SessionChatKey, json);
    }

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
        var builder = new AiPromptBuilder()
            .WithSystemRole("You are an expert personal finance assistant inside FinanceWizard.")
            .WithUserQuestion(model.UserQuestion);

        if (model.IncludeTransactions && userId > 0)
        {
            // Не се променя съществуващият договор: използва се GetAllByUserIdAsync
            var allTransactions = await _transactionService.GetAllByUserIdAsync(userId);
            var recentTx = allTransactions.OrderByDescending(t => t.Date).Take(10);
            builder.WithAnonymizedTransactions(recentTx);
        }

        var prompt = builder.Build();

        history.Add(new ChatMessageDto { Sender = "User", Message = model.UserQuestion });

        var reply = await _aiProvider.GetResponseAsync(prompt, cancellationToken);
        history.Add(new ChatMessageDto { Sender = "AI", Message = reply });

        SaveChatHistory(history);
        model.ConversationHistory = history;
        model.UserQuestion = string.Empty;
        ModelState.Clear();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ClearHistory()
    {
        HttpContext.Session.Remove(SessionChatKey);
        return RedirectToAction(nameof(Index));
    }
}