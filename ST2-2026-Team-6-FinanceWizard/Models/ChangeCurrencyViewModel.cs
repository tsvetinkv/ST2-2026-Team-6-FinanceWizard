using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Input model for the "Change currency" sub-form on the profile page.
    /// Uses a Required validator so the user cannot submit an empty selection.
    /// </summary>
    public class ChangeCurrencyViewModel
    {
        [Required(ErrorMessage = "Choose a currency")]
        public string Currency { get; set; } = string.Empty;
    }
}
