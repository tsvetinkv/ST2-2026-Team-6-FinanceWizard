using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Enter your password to confirm")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
