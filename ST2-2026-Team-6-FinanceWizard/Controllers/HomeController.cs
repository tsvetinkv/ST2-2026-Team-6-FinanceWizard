using Microsoft.AspNetCore.Mvc;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Diagnostics;
using System.Security.Claims;

namespace ST2_2026_Team_6_FinanceWizard.Controllers
{
    /// <summary>
    /// Landing page (for guests) and dashboard (for signed-in users).
    ///
    /// Design patterns used:
    /// - Dependency Injection: IDashboardFacade is injected.
    /// - Facade pattern: IDashboardFacade hides the complexity of pulling data from
    ///   wallets + transactions + stats + repositories behind one method call.
    /// - MVC Controller: this action has very little logic — it delegates to the facade.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDashboardFacade _dashboardFacade;

        public HomeController(ILogger<HomeController> logger, IDashboardFacade dashboardFacade)
        {
            _logger = logger;
            _dashboardFacade = dashboardFacade;
        }

        // The dashboard is never cached (not by the browser, its back/forward cache or a proxy),
        // so after adding, editing or deleting a transaction the numbers are always up to date.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index(string? currency)
        {
            // Guests see the landing page
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return View();
            }

            // Signed-in users see their dashboard: balances, recent transactions, KPIs and charts
            long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Facade pattern: one call returns everything the dashboard needs.
            // The optional "currency" parameter filters the KPIs and charts to one currency.
            var data = await _dashboardFacade.GetDashboardDataAsync(userId, currency);
            return View(HomeDashboardViewModel.FromDashboardData(data));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
