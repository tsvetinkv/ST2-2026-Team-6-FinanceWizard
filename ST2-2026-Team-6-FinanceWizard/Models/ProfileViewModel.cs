namespace ST2_2026_Team_6_FinanceWizard.Models
{
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