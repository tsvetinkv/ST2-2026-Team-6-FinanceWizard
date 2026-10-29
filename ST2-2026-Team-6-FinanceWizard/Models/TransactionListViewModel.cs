using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// View-model for the transaction list page.
    ///
    /// Design notes:
    /// - This model is both INPUT and OUTPUT:
    ///   • INPUT:  FromDate / ToDate / SearchTerm are posted by the filter form.
    ///   • OUTPUT: Transactions / CurrentPage / TotalPages / Wallets are filled by the controller.
    /// - The controller passes this model to the view, so the filter fields stay populated.
    /// </summary>
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
