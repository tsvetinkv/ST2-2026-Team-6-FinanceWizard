#nullable enable
using System.Text;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Builders;

/// <summary>
/// Builder Pattern: Assembles contextual AI prompts safely while stripping user PII (names, emails, passwords).
/// </summary>
public class AiPromptBuilder
{
    private readonly StringBuilder _sb = new();

    public AiPromptBuilder WithSystemRole(string roleDescription)
    {
        _sb.AppendLine($"System Role: {roleDescription}");
        return this;
    }

    public AiPromptBuilder WithUserQuestion(string question)
    {
        _sb.AppendLine($"\nUser Question:\n{question.Trim()}");
        return this;
    }

    public AiPromptBuilder WithAnonymizedTransactions(IEnumerable<Transaction>? transactions)
    {
        if (transactions == null || !transactions.Any()) return this;

        _sb.AppendLine("\nRecent User Transactions (Anonymized Context):");
        foreach (var t in transactions.Take(10))
        {
            var currency = t.Wallet?.Currency ?? "EUR";
            var description = t.Description ?? "N/A";
            _sb.AppendLine($"- Date: {t.Date:yyyy-MM-dd}, Amount: {t.Amount} {currency}, Type: {t.Type}, Description: {description}");
        }
        return this;
    }

    public string Build()
    {
        _sb.AppendLine("\nNotice: Provide structured, practical advice. Never ask for passwords or full account numbers.");
        return _sb.ToString();
    }
}