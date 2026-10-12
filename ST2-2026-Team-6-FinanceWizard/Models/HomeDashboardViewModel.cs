using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    public class HomeDashboardViewModel
    {
        public List<Wallet> Wallets { get; set; } = new();
        public List<Transaction> RecentTransactions { get; set; } = new();

        public int TransactionCount { get; set; }
        public int MerchantCount { get; set; }
        public int CategoryCount { get; set; }
        public int TagCount { get; set; }
    }
}
