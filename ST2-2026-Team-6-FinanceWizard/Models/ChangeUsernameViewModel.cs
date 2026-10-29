using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Input model for the "Change username" sub-form.
    /// Length is bounded so it fits the DB column and stays reasonable in the navbar.
    /// </summary>
    public class ChangeUsernameViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Username must be between 2 and 50 characters")]
        public string Username { get; set; } = string.Empty;
    }
}
