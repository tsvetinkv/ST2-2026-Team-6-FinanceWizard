using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ST2_2026_Team_6_FinanceWizard.Views.Assistant
{
    /// <summary>
    /// Razor Page model scaffolded for Views/Assistant/Index.cshtml.
    ///
    /// Design notes:
    /// - In this project the assistant page is actually served by AssistantController (MVC),
    ///   not by this PageModel. The file exists because the default template creates it,
    ///   and it is harmless: no route maps to it because there is no @page directive in the view.
    /// - The empty OnGet() is here so the file compiles; if the MVC controller is ever removed
    ///   in favour of a Razor Page, this is the model that would back the page.
    /// </summary>
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
