using System.Text;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Builders;

public sealed class AiPromptBuilder
{
    private readonly StringBuilder _sb = new();

    public AiPromptBuilder()
    {
        _sb.AppendLine("You are FinanceWizard AI — an intelligent financial assistant.");
        _sb.AppendLine("Let’s give brief, practical, and accurate personal finance advice in English.");
        _sb.AppendLine("MANDATORY RULE: End every response with the sentence: ‘Note: The information provided is for informational purposes only and does not constitute official financial advice.’'");
        _sb.AppendLine();
    }

    public AiPromptBuilder AddWalletContext(Wallet? wallet)
    {
        if (wallet != null)
        {
            _sb.AppendLine("[Financial portfolio context]");
            _sb.AppendLine($"- Balance: {wallet.Balance:F2} {wallet.Currency}");
            _sb.AppendLine();
        }
        return this;
    }

    public AiPromptBuilder AddRecentTransactions(IEnumerable<Transaction>? transactions)
    {
        if (transactions != null && transactions.Any())
        {
            _sb.AppendLine("[Anonymized recent transactions]");
            foreach (var t in transactions)
            {
                _sb.AppendLine($"- Date: {t.Date:yyyy-MM-dd} | Type: {t.Type} | Amount: {t.Amount:F2} | Description: {t.Description}");
            }
            _sb.AppendLine();
        }
        return this;
    }

    public AiPromptBuilder AddUserQuestion(string question)
    {
        _sb.AppendLine("[User’s question]");
        _sb.AppendLine(question.Trim());
        return this;
    }

    public string Build() => _sb.ToString();
}