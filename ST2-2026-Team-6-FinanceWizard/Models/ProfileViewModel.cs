namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Composite view-model for the profile page.
    ///
    /// Design notes:
    /// - The page has multiple independent forms. Each form has its own sub-model,
    ///   so when one form fails validation the others' values are not affected.
    /// - The controller binds each sub-form with a "Prefix" so the field names do not collide
    ///   (e.g. ChangeUsername.Username vs ChangePassword.CurrentPassword).
    /// </summary>
    public class ProfileViewModel
    {
        // Display only
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "EUR";
        public string? PhotoUrl { get; set; }

        // One sub-model per form
        public ChangeUsernameViewModel ChangeUsername { get; set; } = new();
        public ChangePasswordViewModel ChangePassword { get; set; } = new();
        public ChangeCurrencyViewModel ChangeCurrency { get; set; } = new();
        public DeleteAccountViewModel DeleteAccount { get; set; } = new();
    }
}
