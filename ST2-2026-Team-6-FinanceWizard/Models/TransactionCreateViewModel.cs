using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
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
    }
}
