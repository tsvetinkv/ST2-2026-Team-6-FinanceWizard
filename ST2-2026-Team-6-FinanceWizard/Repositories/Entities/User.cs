using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public Wallet Wallet { get; set; }
    }
}