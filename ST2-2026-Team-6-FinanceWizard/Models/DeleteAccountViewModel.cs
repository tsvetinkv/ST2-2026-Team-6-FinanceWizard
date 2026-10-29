using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Input model for the "Delete account" confirmation form.
    /// The user must retype their password so a click cannot accidentally (or maliciously) delete the account.
    /// </summary>
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Enter your password to confirm")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
