using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    public class TransactionListViewModel
    {
        public List<Transaction> Transactions { get; set; } = new();

        // Filters
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }

        // The user's wallets (spending / savings)
        public List<Wallet> Wallets { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
    }
}
