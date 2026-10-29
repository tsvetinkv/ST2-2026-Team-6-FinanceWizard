using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Form model for creating / editing a transaction.
    ///
    /// Design notes:
    /// - The dropdown lists (Wallets, Merchants, Categories, Tags) are marked with [ValidateNever]
    ///   because they are populated by the controller and never posted by the client.
    ///   Without [ValidateNever], model binding would try to validate these nested objects
    ///   and fail with "The value is not valid".
    /// - Type is restricted to the two allowed values with [RegularExpression], so a tampered
    ///   request cannot store an arbitrary string.
    /// </summary>
    public class TransactionCreateViewModel
    {
        [Required(ErrorMessage = "Please enter an amount.")]
        [Range(0.01, 1000000, ErrorMessage = "The amount must be between 0.01 and 1,000,000.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a transaction type.")]
        [RegularExpression("^(Income|Expense)$", ErrorMessage = "Invalid transaction type.")]
        [Display(Name = "Type")]
        public string Type { get; set; } = "Expense"; // Income / Expense

        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Now;

        // --- Single choices ---
        [Required(ErrorMessage = "Please choose a wallet.")]
        [Display(Name = "Wallet")]
        public long? WalletId { get; set; }

        [Display(Name = "Merchant")]
        public long? MerchantId { get; set; }

        [Display(Name = "Category")]
        public long? CategoryId { get; set; }

        // --- Multiple choice ---
        [Display(Name = "Tags")]
        public List<long> TagIds { get; set; } = new();

        // --- Options for the form (filled by the controller, never posted) ---
        [ValidateNever] public List<Wallet> Wallets { get; set; } = new();
        [ValidateNever] public List<Merchant> Merchants { get; set; } = new();
        [ValidateNever] public List<Category> Categories { get; set; } = new();
        [ValidateNever] public List<Tag> Tags { get; set; } = new();
    }
}
