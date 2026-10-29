using System.ComponentModel.DataAnnotations;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Input model for the "Change password" sub-form.
    ///
    /// Validators:
    /// - [DataType(Password)] masks the field in the rendered HTML.
    /// - [MinLength(6)] enforces the password policy.
    /// - [Compare(...)] ensures the two new-password fields match.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Enter your current password")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a new password")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm your new password")]
        [Compare("NewPassword", ErrorMessage = "Passwords don't match")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
