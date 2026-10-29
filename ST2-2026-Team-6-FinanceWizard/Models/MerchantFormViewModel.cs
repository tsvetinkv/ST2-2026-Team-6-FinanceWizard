namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Form model for adding / editing a merchant.
    ///
    /// Design notes:
    /// - LogoUrl is intentionally not bound from the client: the logo can only be
    ///   set by uploading a file or by providing an ImageLink, so the client cannot
    ///   inject an arbitrary URL directly.
    /// - CurrentLogoUrl is used only in Edit mode to display the existing logo.
    /// - ValidateLogoChoice in the controller enforces "photo XOR link".
    /// </summary>
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
