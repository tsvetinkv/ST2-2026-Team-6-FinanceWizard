using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using ST2_2026_Team_6_FinanceWizard.Services.Models;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    // Rule-based "AI" category suggestion. It works in two steps:
    //   1. History  - the user already categorised a transaction with the same description (or merchant):
    //                 reuse the category they chose most often.
    //   2. Keywords - well-known shops / services ("Starbucks", "Uber", "Billa", ...) are mapped to a general
    //                 concept (Food & Coffee, Transport, Groceries, ...). The concept is then matched
    //                 against the names of the user's own categories.
    // The interface makes it easy to replace this class with a real LLM call later.
    public class AiCategoryService : IAiCategoryService
    {
        private const int MaxTextLength = 500;

        // Concept = general idea; Aliases = category names that fit it, best first;
        // Keywords = words in a description / merchant name that point to it
        private sealed record Rule(string Concept, string[] Aliases, string[] Keywords);

        private static readonly Rule[] Rules =
        {
            new("Food & Coffee",
                new[] { "food & coffee", "coffee", "cafe", "restaurants", "restaurant", "dining", "eating out", "food & drink", "food" },
                new[] { "starbucks", "costa", "coffee", "cafe", "café", "espresso", "latte", "cappuccino", "mcdonald", "burger king", "kfc",
                        "subway", "pizza", "domino", "sushi", "restaurant", "bistro", "bakery", "glovo", "takeaway", "foodpanda", "wolt",
                        "lunch", "dinner", "breakfast", "кафе", "кафене", "ресторант", "пица", "закуска", "обяд", "вечеря" }),

            new("Groceries",
                new[] { "groceries", "grocery", "supermarket", "food", "shopping" },
                new[] { "billa", "lidl", "kaufland", "fantastico", "carrefour", "aldi", "tesco", "spar", "supermarket", "grocery", "groceries",
                        "t market", "билла", "лидл", "кауфланд", "фантастико", "хранителни", "супермаркет" }),

            new("Transport",
                new[] { "transport", "transportation", "commute", "taxi", "travel", "car" },
                new[] { "uber", "bolt", "taxi", "lyft", "bus", "tram", "metro", "train", "bdz", "parking", "toll", "vignette",
                        "бдж", "такси", "автобус", "паркинг", "винетка", "градски транспорт" }),

            new("Fuel",
                new[] { "fuel", "gas", "petrol", "car", "transport" },
                new[] { "shell", "omv", "lukoil", "eko", "petrol", "gas station", "diesel", "fuel", "бензин", "дизел", "лукойл", "горива" }),

            new("Bills & Services",
                new[] { "bills & services", "bills", "utilities", "utility", "services" },
                new[] { "evn", "electricity", "water", "heating", "toplofikacia", "internet", "vivacom", "a1", "yettel", "telenor",
                        "phone bill", "insurance", "vik", "ток", "вода", "парно", "интернет", "застраховка", "сметка" }),

            new("Housing",
                new[] { "housing", "rent", "home", "bills & services" },
                new[] { "rent", "mortgage", "landlord", "ikea", "наем", "ипотека", "наемодател" }),

            new("Entertainment",
                new[] { "entertainment", "leisure", "fun", "subscriptions" },
                new[] { "netflix", "spotify", "hbo", "disney", "youtube", "cinema", "movie", "theatre", "theater", "concert", "steam",
                        "playstation", "xbox", "кино", "театър", "концерт" }),

            new("Shopping",
                new[] { "shopping", "clothes", "clothing", "retail" },
                new[] { "amazon", "emag", "zara", "decathlon", "zalando", "aboutyou", "clothes", "shoes", "fashion", "mall",
                        "дрехи", "обувки", "мол" }),

            new("Health",
                new[] { "health", "healthcare", "pharmacy", "medical", "medicine" },
                new[] { "pharmacy", "sopharma", "subra", "remedium", "dentist", "doctor", "clinic", "hospital",
                        "аптека", "зъболекар", "лекар", "болница", "клиника" }),

            new("Sports & Fitness",
                new[] { "fitness", "sports", "sport", "health" },
                new[] { "gym", "fitness", "pulse fitness", "yoga", "pilates", "фитнес", "спортна зала" }),

            new("Travel",
                new[] { "travel", "holiday", "vacation", "trips" },
                new[] { "booking", "airbnb", "ryanair", "wizz", "wizzair", "flight", "hotel", "hostel", "airline",
                        "хотел", "самолетен билет", "почивка" }),

            new("Education",
                new[] { "education", "learning", "courses", "books" },
                new[] { "udemy", "coursera", "tuition", "course", "university", "book", "books", "курс", "университет", "книги" }),

            new("Salary",
                new[] { "salary", "income", "wages", "work" },
                new[] { "salary", "payroll", "wage", "bonus", "freelance", "invoice", "заплата", "бонус" })
        };

        private static readonly Regex NonWord = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);
        private static readonly string[] StopWords = { "and", "the", "for" };

        private readonly IEfRepository<Category> _categoryRepo;
        private readonly IEfRepository<Transaction> _transactionRepo;

        public AiCategoryService(IEfRepository<Category> categoryRepo, IEfRepository<Transaction> transactionRepo)
        {
            _categoryRepo = categoryRepo;
            _transactionRepo = transactionRepo;
        }

        public async Task<CategorySuggestion> SuggestAsync(
            long userId,
            string? description,
            string? merchantName,
            CancellationToken cancellationToken = default)
        {
            string desc = Clip(description);
            string merchant = Clip(merchantName);
            if (desc.Length == 0 && merchant.Length == 0)
            {
                return CategorySuggestion.None;
            }

            var categories = await _categoryRepo.All().AsNoTracking().ToListAsync(cancellationToken);
            if (categories.Count == 0)
            {
                // Still tell the user what the text looks like, so the UI can offer to create it
                return SuggestFromKeywords(desc, merchant, categories);
            }

            // 1. Same text used before? Then the user's own choice wins.
            var fromHistory = await SuggestFromHistoryAsync(userId, desc, merchant, categories, cancellationToken);
            if (fromHistory != null)
            {
                return fromHistory;
            }

            // 2. Keyword rules
            return SuggestFromKeywords(desc, merchant, categories);
        }

        // ---------------------------------------------------------------- history

        private async Task<CategorySuggestion?> SuggestFromHistoryAsync(
            long userId, string desc, string merchant, List<Category> categories, CancellationToken ct)
        {
            var query = _transactionRepo.All()
                .AsNoTracking()
                .Where(t => t.Wallet.UserId == userId && t.CategoryId != null);

            if (desc.Length > 0)
            {
                query = query.Where(t => t.Description == desc);
            }
            else
            {
                query = query.Where(t => t.Merchant != null && t.Merchant.Name == merchant);
            }

            var top = await query
                .GroupBy(t => t.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync(ct);

            var category = top == null ? null : categories.FirstOrDefault(c => c.Id == top.CategoryId);
            if (top == null || category == null)
            {
                return null;
            }

            double confidence = Math.Min(0.99, 0.9 + Math.Min(top.Count, 5) * 0.01);
            return new CategorySuggestion(
                category.Id,
                category.Name,
                category.Name,
                confidence,
                "history",
                top.Count == 1
                    ? "You used this category for the same text before."
                    : $"You used this category {top.Count} times for the same text.");
        }

        // ---------------------------------------------------------------- keywords

        private static CategorySuggestion SuggestFromKeywords(string desc, string merchant, List<Category> categories)
        {
            string descText = Normalize(desc);
            string merchantText = Normalize(merchant);

            // All rules that match, best first: a longer keyword is more specific; ties keep the rule order
            var matches = new List<(Rule Rule, string Keyword, bool FromMerchant, int Order)>();
            for (int i = 0; i < Rules.Length; i++)
            {
                string? inMerchant = LongestMatch(Rules[i], merchantText);
                string? inDesc = LongestMatch(Rules[i], descText);
                string? best = inMerchant != null && (inDesc == null || inMerchant.Length >= inDesc.Length) ? inMerchant : inDesc;
                if (best != null)
                {
                    matches.Add((Rules[i], best, inMerchant != null, i));
                }
            }

            if (matches.Count == 0)
            {
                return CategorySuggestion.None;
            }

            var ordered = matches.OrderByDescending(m => m.Keyword.Length).ThenBy(m => m.Order).ToList();

            foreach (var m in ordered)
            {
                var (category, aliasIndex) = Resolve(m.Rule, categories);
                if (category == null)
                {
                    continue;
                }

                double confidence = m.FromMerchant ? 0.85 : 0.8;
                if (aliasIndex > 0) confidence -= 0.1; // the user's category is only a looser fit for the concept

                return new CategorySuggestion(
                    category.Id,
                    category.Name,
                    m.Rule.Concept,
                    confidence,
                    "keyword",
                    $"\"{m.Keyword}\" usually belongs to {m.Rule.Concept}.");
            }

            // Something was recognised, but the user has no matching category yet
            var first = ordered[0];
            return new CategorySuggestion(
                null,
                null,
                first.Rule.Concept,
                0.5,
                "keyword",
                $"\"{first.Keyword}\" usually belongs to {first.Rule.Concept}.");
        }

        // Longest keyword of the rule that occurs in the (normalised) text
        private static string? LongestMatch(Rule rule, string text)
        {
            if (text.Length == 0) return null;

            string? best = null;
            foreach (var keyword in rule.Keywords)
            {
                string k = NonWord.Replace(keyword.ToLowerInvariant(), " ").Trim();
                if (k.Length == 0) continue;

                // Short keywords ("bus", "uber") must be whole words, longer ones may be a word start ("restaurants")
                bool hit = k.Length >= 5
                    ? text.Contains(" " + k, StringComparison.Ordinal)
                    : text.Contains(" " + k + " ", StringComparison.Ordinal);

                if (hit && (best == null || k.Length > best.Length))
                {
                    best = k;
                }
            }
            return best;
        }

        // Finds the user's category that best fits a rule: exact alias name first, then shared words
        private static (Category? Category, int AliasIndex) Resolve(Rule rule, List<Category> categories)
        {
            Category? bestCategory = null;
            int bestAlias = 0;
            int bestScore = 0;

            for (int i = 0; i < rule.Aliases.Length; i++)
            {
                string alias = rule.Aliases[i];
                string aliasSquashed = Squash(alias);
                var aliasTokens = Tokens(alias);

                foreach (var category in categories)
                {
                    int score = 0;
                    if (Squash(category.Name) == aliasSquashed)
                    {
                        score = 1000 - i;
                    }
                    else if (Tokens(category.Name).Overlaps(aliasTokens))
                    {
                        score = 500 - i;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCategory = category;
                        bestAlias = i;
                    }
                }
            }

            return (bestCategory, bestAlias);
        }

        // ---------------------------------------------------------------- helpers

        private static string Clip(string? text)
        {
            var t = (text ?? string.Empty).Trim();
            return t.Length > MaxTextLength ? t[..MaxTextLength] : t;
        }

        // " starbucks coffee sofia " - lower case, punctuation removed, padded so whole words can be searched
        private static string Normalize(string text)
        {
            string cleaned = NonWord.Replace(text.ToLowerInvariant(), " ").Trim();
            return cleaned.Length == 0 ? string.Empty : " " + cleaned + " ";
        }

        // Letters and digits only: "Bills & Services" -> "billsservices"
        private static string Squash(string text) => NonWord.Replace(text.ToLowerInvariant(), string.Empty);

        // Words of a name without filler words, with a simple plural stripped ("Restaurants" -> "restaurant")
        private static HashSet<string> Tokens(string text)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var word in NonWord.Split(text.ToLowerInvariant()))
            {
                if (word.Length < 3 || StopWords.Contains(word)) continue;
                set.Add(word.Length > 3 && word.EndsWith('s') ? word[..^1] : word);
            }
            return set;
        }
    }
}
