using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities
{
    /// <summary>
    /// Application user. Mapped to the "Users" table.
    ///
    /// Design notes:
    /// - PasswordHash stores a BCrypt hash, not the plain password.
    /// - Wallets is the 1:N navigation; cascade delete (configured in ApplicationDbContext)
    ///   means deleting a User also deletes all their wallets and their wallets' transactions.
    /// </summary>
    [Table("Users")]
    public class User
    {
        [Key]
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        // A user can have several wallets (spending / savings)
        public ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
    }
}
