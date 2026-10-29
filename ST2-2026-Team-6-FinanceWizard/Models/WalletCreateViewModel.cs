using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Form model for creating / editing a wallet.
    ///
    /// Design notes:
    /// - The Type field uses [RegularExpression] to lock it to the two valid WalletTypes values.
    /// - Balance is allowed to be 0 (new empty wallet) but never negative.
    /// - Currency is validated against CurrencyInfo.Find in the controller, not by data annotations,
    ///   because the supported set lives in that class.
    /// </summary>
    public class WalletCreateViewModel
    {
        [Required(ErrorMessage = "Please enter a wallet name.")]
        [StringLength(60, ErrorMessage = "The name cannot exceed 60 characters.")]
        [Display(Name = "Wallet name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Spending|Savings)$", ErrorMessage = "Invalid wallet type.")]
        [Display(Name = "Type")]
        public string Type { get; set; } = WalletTypes.Spending;

        [Range(0, 1000000000, ErrorMessage = "The initial amount cannot be negative.")]
        [Display(Name = "Initial amount")]
        public decimal Balance { get; set; }

        [Required(ErrorMessage = "Please choose a currency.")]
        [StringLength(8)]
        [Display(Name = "Currency")]
        public string Currency { get; set; } = "EUR";
    }
}
