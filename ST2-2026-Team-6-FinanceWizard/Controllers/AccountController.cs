using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Globalization;
using System.Security.Claims;

namespace ST2_2026_Team_6_FinanceWizard.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IWalletService _walletService;
        private readonly IProfilePhotoService _photos;

        public AccountController(IUserService userService, IWalletService walletService, IProfilePhotoService photos)
        {
            _userService = userService;
            _walletService = walletService;
            _photos = photos;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userService.ValidateUserAsync(model.Email, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var created = await _userService.RegisterAsync(model.Name, model.Email, model.Password);
                if (created == null)
                {
                    ModelState.AddModelError("", "Registration failed. Please try again.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Registration successful. You can now sign in.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // ======================= PROFILE =======================

        private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var vm = await BuildProfileAsync();
            if (vm == null) return await SignOutAndRedirectAsync();
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUsername(
            [Bind(Prefix = "ChangeUsername")] ChangeUsernameViewModel model)
        {
            if (!ModelState.IsValid)
                return await ProfileWithErrorsAsync(vm => vm.ChangeUsername = model);

            var user = await _userService.UpdateUsernameAsync(CurrentUserId, model.Username.Trim());
            if (user == null) return await SignOutAndRedirectAsync();

            await SignInUserAsync(user.Id, user.Username, user.Email); // refresh the name in the navbar

            TempData["ProfileSuccess"] = "Username updated.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            [Bind(Prefix = "ChangePassword")] ChangePasswordViewModel model)
        {
            if (ModelState.IsValid && model.CurrentPassword == model.NewPassword)
                ModelState.AddModelError("ChangePassword.NewPassword", "The new password must be different from the current one.");

            if (!ModelState.IsValid)
                return await ProfileWithErrorsAsync(vm => vm.ChangePassword = model);

            var ok = await _userService.ChangePasswordAsync(CurrentUserId, model.CurrentPassword, model.NewPassword);
            if (!ok)
            {
                ModelState.AddModelError("ChangePassword.CurrentPassword", "Current password is incorrect.");
                return await ProfileWithErrorsAsync(vm => vm.ChangePassword = model);
            }

            TempData["ProfileSuccess"] = "Password changed.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeCurrency(
    [Bind(Prefix = "ChangeCurrency")] ChangeCurrencyViewModel model)
        {
            if (!string.IsNullOrEmpty(model.Currency) && CurrencyInfo.Find(model.Currency) == null)
                ModelState.AddModelError("ChangeCurrency.Currency", "Unsupported currency.");

            if (!ModelState.IsValid)
                return await ProfileWithErrorsAsync(vm => vm.ChangeCurrency = model);

            var wallet = await _walletService.GetMainWalletAsync(CurrentUserId);
            if (wallet == null)
            {
                ModelState.AddModelError("ChangeCurrency.Currency", "You don't have a wallet yet.");
                return await ProfileWithErrorsAsync(vm => vm.ChangeCurrency = model);
            }

            var result = await _walletService.ChangeCurrencyAsync(CurrentUserId, wallet.Id, model.Currency);
            if (!result.Success)
            {
                ModelState.AddModelError("ChangeCurrency.Currency", result.Error!);
                return await ProfileWithErrorsAsync(vm => vm.ChangeCurrency = model);
            }

            var rate = result.Rate.ToString("0.######", CultureInfo.InvariantCulture);
            TempData["ProfileSuccess"] =
                result.FromCode == result.ToCode
                    ? "Currency updated."
                : !result.Converted
                    ? $"Main wallet currency changed to {result.ToCode}. The wallet had no money or transactions, so nothing was converted."
                    : $"Main wallet converted to {result.ToCode} at 1 {result.FromCode} = {rate} {result.ToCode}. Your other wallets were not changed.";

            return RedirectToAction(nameof(Profile));
        }

        // Live preview used by the currency picker (nothing is saved)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ExchangeRate(string to)
        {
            var wallet = await _walletService.GetMainWalletAsync(CurrentUserId);
            if (wallet == null) return BadRequest(new { error = "You don't have a wallet yet." });

            var p = await _walletService.PreviewCurrencyChangeAsync(CurrentUserId, wallet.Id, to);
            if (!p.Success) return BadRequest(new { error = p.Error });

            return Json(new { from = p.FromCode, to = p.ToCode, rate = p.Rate, before = p.Before, after = p.After });
        }



        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile? photo)
        {
            if (photo == null)
            {
                ModelState.AddModelError("Photo", "Choose an image first.");
                return await ProfileWithErrorsAsync(_ => { });
            }

            var error = await _photos.SaveAsync(CurrentUserId, photo);
            if (error != null)
            {
                ModelState.AddModelError("Photo", error);
                return await ProfileWithErrorsAsync(_ => { });
            }

            TempData["ProfileSuccess"] = "Profile photo updated.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemovePhoto()
        {
            _photos.Delete(CurrentUserId);
            TempData["ProfileSuccess"] = "Profile photo removed.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(
            [Bind(Prefix = "DeleteAccount")] DeleteAccountViewModel model)
        {
            if (!ModelState.IsValid)
                return await ProfileWithErrorsAsync(vm => vm.DeleteAccount = model);

            var userId = CurrentUserId;
            var deleted = await _userService.DeleteAccountAsync(userId, model.Password);
            if (!deleted)
            {
                ModelState.AddModelError("DeleteAccount.Password", "Incorrect password.");
                return await ProfileWithErrorsAsync(vm => vm.DeleteAccount = model);
            }

            _photos.Delete(userId);
            return await SignOutAndRedirectAsync();
        }

        // ---------- helpers ----------

        private async Task<ProfileViewModel?> BuildProfileAsync()
        {
            var user = await _userService.GetByIdAsync(CurrentUserId);
            if (user == null) return null;

            var main = await _walletService.GetMainWalletAsync(user.Id);
            var code = CurrencyInfo.Find(main?.Currency)?.Code;

            return new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                CurrencyCode = code ?? "N/A",
                PhotoUrl = _photos.GetUrl(user.Id),
                ChangeUsername = new ChangeUsernameViewModel { Username = user.Username },
                ChangeCurrency = new ChangeCurrencyViewModel { Currency = code ?? string.Empty }
            };
        }
        private async Task<IActionResult> ProfileWithErrorsAsync(Action<ProfileViewModel> applyPostedSection)
        {
            var vm = await BuildProfileAsync();
            if (vm == null) return await SignOutAndRedirectAsync();

            applyPostedSection(vm);
            return View("Profile", vm);
        }

        private async Task<IActionResult> SignOutAndRedirectAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private async Task SignInUserAsync(long id, string username, string email)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, id.ToString()),
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Email, email)
    };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }
    }
}
