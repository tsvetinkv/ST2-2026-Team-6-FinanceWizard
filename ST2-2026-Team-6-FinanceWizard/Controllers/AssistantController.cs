using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Builders;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

[Authorize]
public class AssistantController : Controller
{
    private readonly IAiProvider _aiProvider;
    private readonly IWalletService _walletService;
    private readonly IEfRepository<Transaction> _transactionRepo;

    public AssistantController(
        IAiProvider aiProvider,
        IWalletService walletService,
        IEfRepository<Transaction> transactionRepo)
    {
        _aiProvider = aiProvider;
        _walletService = walletService;
        _transactionRepo = transactionRepo;
    }

    private long GetUserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public IActionResult Index()
    {
        return View(new AssistantViewModel { ActiveProvider = _aiProvider.ProviderName });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(AssistantViewModel model, CancellationToken cancellationToken)
    {
        model.ActiveProvider = _aiProvider.ProviderName;

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        long userId = GetUserId();
        var wallet = await _walletService.GetByUserIdAsync(userId);

        IEnumerable<Transaction>? recentTransactions = null;
        if (model.IncludeTransactions && wallet != null)
        {
            recentTransactions = _transactionRepo.All()
                .Where(t => t.WalletId == wallet.Id)
                .OrderByDescending(t => t.Date)
                .Take(10)
                .ToList();
        }

        string prompt = new AiPromptBuilder()
            .AddWalletContext(wallet)
            .AddRecentTransactions(recentTransactions)
            .AddUserQuestion(model.UserQuestion)
            .Build();

        string aiAnswer = await _aiProvider.GetResponseAsync(prompt, cancellationToken);

        model.ConversationHistory.Add(new ChatMessageDto
        {
            Sender = "User",
            Message = model.UserQuestion,
            Timestamp = DateTime.Now
        });

        model.ConversationHistory.Add(new ChatMessageDto
        {
            Sender = "AI",
            Message = aiAnswer,
            Timestamp = DateTime.Now
        });

        model.UserQuestion = string.Empty;

        return View("Index", model);
    }
}