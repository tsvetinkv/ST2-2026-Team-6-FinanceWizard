namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Simple view-model for the global error page.
    /// ShowRequestId hides the request id block when there is no id to display.
    /// </summary>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
