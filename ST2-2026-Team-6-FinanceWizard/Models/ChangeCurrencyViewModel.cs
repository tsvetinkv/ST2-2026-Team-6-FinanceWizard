using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    public class ChangeCurrencyViewModel
    {
        [Required(ErrorMessage = "Choose a currency")]
        public string Currency { get; set; } = string.Empty;
    }
}
