using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities
{
    [Table("Wallets")]
    public class Wallet
    {
        [Key]
        public long Id { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "€";
        public long UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
