#nullable enable
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using System.Text;

namespace ST2_2026_Team_6_FinanceWizard.Services.Builders;

/// <summary>
/// Builder Pattern: Assembles contextual AI prompts safely while stripping user PII (names, emails, passwords).
///
/// Why Builder here:
/// - A prompt is assembled from several optional parts (system role, user question, transactions, footer).
/// - Without a builder, the calling code would have to know the order and formatting of each section.
/// - With the builder, the caller chains readable method calls (.WithSystemRole().WithUserQuestion().Build())
///   and the builder is responsible for producing a well-formed prompt.
/// - The fluent API returns "this" from each method, so the calls can be chained.
///
/// Privacy: the builder only accepts Wallet.Currency and Transaction data (amount / type / description / date).
/// It never touches User.Username, User.Email or PasswordHash, so those never leave the server.
/// </summary>
public class AiPromptBuilder
{
    // The prompt is accumulated in a StringBuilder, one section per method.
    private readonly StringBuilder _sb = new();

    /// <summary>
    /// Adds the "System Role" preamble that instructs the LLM what its job is.
    /// Usually the first section of the prompt.
    /// </summary>
    public AiPromptBuilder WithSystemRole(string roleDescription)
    {
        _sb.AppendLine($"System Role: {roleDescription}");
        return this; // fluent API: allow chaining
    }

    /// <summary>
    /// Adds the user's actual question. Trimmed so trailing whitespace doesn't pollute the prompt.
    /// </summary>
    public AiPromptBuilder WithUserQuestion(string question)
    {
        _sb.AppendLine($"\nUser Question:\n{question.Trim()}");
        return this;
    }

    /// <summary>
    /// Adds up to 10 recent transactions as context for the LLM.
    /// - Null or empty collections are silently skipped (nothing to add).
    /// - Only safe fields are included: date, amount, currency, type, description.
    /// - The Take(10) is a hard cap, so the prompt never becomes huge even for very active users.
    /// </summary>
    public AiPromptBuilder WithAnonymizedTransactions(IEnumerable<Transaction>? transactions)
    {
        if (transactions == null || !transactions.Any()) return this;

        _sb.AppendLine("\nRecent User Transactions (Anonymized Context):");
        foreach (var t in transactions.Take(10))
        {
            // Currency is read from the related Wallet. If the Wallet wasn't loaded,
            // fall back to EUR so the prompt still renders a valid number.
            var currency = t.Wallet?.Currency ?? "EUR";
            var description = t.Description ?? "N/A";
            _sb.AppendLine($"- Date: {t.Date:yyyy-MM-dd}, Amount: {t.Amount} {currency}, Type: {t.Type}, Description: {description}");
        }
        return this;
    }

    /// <summary>
    /// Finalises the prompt: appends a fixed footer (behavioural notice) and returns the full string.
    /// The notice asks the model to avoid requesting sensitive information and to reply in a structured way.
    /// </summary>
    public string Build()
    {
        _sb.AppendLine("\nNotice: Provide structured, practical advice. Never ask for passwords or full account numbers.");
        return _sb.ToString();
    }
}
