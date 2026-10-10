using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    public class TransactionCreateViewModel
    {
        [Required(ErrorMessage = "Въведете сума.")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Въведете валидна положителна сума (напр. 10 или 25.50).")]
        [Range(0.01, 1000000, ErrorMessage = "Сумата трябва да бъде положителна.")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Изберете тип на транзакцията")]
        public string Type { get; set; } = "Expense"; // Income / Expense

        public DateTime Date { get; set; } = DateTime.Now;
    }
}