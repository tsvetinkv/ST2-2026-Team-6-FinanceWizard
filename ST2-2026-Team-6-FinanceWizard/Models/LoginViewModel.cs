using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Input model for the login form.
    /// Both fields are required; the email is validated with [EmailAddress].
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}
