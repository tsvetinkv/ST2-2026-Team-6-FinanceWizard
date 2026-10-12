namespace ST2_2026_Team_6_FinanceWizard.Models
{
    // Form for adding / editing a merchant. The logo can be an uploaded photo OR an image link.
    public class MerchantFormViewModel
    {
        public string Name { get; set; } = string.Empty;

        // Web address of an image to use as the logo
        public string? ImageLink { get; set; }

        // Edit only: remove the logo that is currently set
        public bool RemoveLogo { get; set; }

        // Edit only: logo that is currently saved (shown in the form)
        public string? CurrentLogoUrl { get; set; }

        // Uploaded photo
        public IFormFile? Logo { get; set; }
    }
}
