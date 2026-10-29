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
    /// <summary>
    /// Handles authentication (login/register/logout) and profile management.
    ///
    /// Design patterns used:
    /// - MVC Controller: orchestrates the request flow between views and services.
    /// - Dependency Injection: IUserService, IWalletService and IProfilePhotoService are injected
    ///   through the constructor, keeping the controller decoupled from concrete implementations.
    /// - Claim-based authentication: user identity is stored in a cookie as Claims (NameIdentifier, Name, Email),
    ///   which downstream controllers use to identify the current user.
    /// </summary>
    public class AccountController : Controller
    {
        // Services are injected through the constructor (Dependency Injection pattern).
        // Each service handles a single responsibility: user CRUD, wallet operations and photo storage.
        private readonly IUserService _userService;
        private readonly IWalletService _walletService;
        private readonly IProfilePhotoService _photos;

        public AccountController(IUserService userService, IWalletService walletService, IProfilePhotoService photos)
        {
            _userService = userService;
            _walletService = walletService;
            _photos = photos;
        }

        // ======================= AUTHENTICATION =======================

        /// <summary>
        /// GET /Account/Login — shows the login form.
        /// </summary>
        [HttpGet]
        public IActionResult Login() => View();

        /// <summary>
        /// POST /Account/Login — validates credentials and signs the user in.
        ///
        /// Flow:
        /// 1. Validate the posted model (data annotations).
        /// 2. Ask IUserService to verify the email + password (BCrypt hash comparison inside the service).
        /// 3. Build a ClaimsIdentity with the user's id, username and email.
        /// 4. Sign in via the cookie authentication scheme.
        /// 5. Redirect to the dashboard.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken] // CSRF protection — the form must include a matching token
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userService.ValidateUserAsync(model.Email, model.Password);
            if (user == null)
            {
                // Do not reveal whether the email exists or the password is wrong — generic message.
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Claims are the pieces of information about the user that get stored in the auth cookie.
            // Every controller can read them later via User.FindFirstValue(...).
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

        /// <summary>
        /// GET /Account/Register — shows the registration form.
        /// </summary>
        [HttpGet]
        public IActionResult Register() => View();

        /// <summary>
        /// POST /Account/Register — creates a new user (and their first wallet).
        ///
        /// The service throws an exception if the email already exists;
        /// the controller translates it into a user-friendly ModelState error.
        /// </summary>
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

                // TempData survives one redirect — used to show a success banner on the login page.
                TempData["SuccessMessage"] = "Registration successful. You can now sign in.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                // Surface the business error (e.g. "User with this email already exists.") to the form.
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// POST /Account/Logout — clears the auth cookie.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // ======================= PROFILE =======================

        /// <summary>
        /// Helper property: extracts the current user's id from the auth cookie claims.
        /// The "!" tells the compiler the value is guaranteed to be non-null for [Authorize] actions.
        /// </summary>
        private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// GET /Account/Profile — shows the user's profile page with all the sub-forms.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var vm = await BuildProfileAsync();
            if (vm == null) return await SignOutAndRedirectAsync();
            return View(vm);
        }

        /// <summary>
        /// POST /Account/Profile/ChangeUsername — updates the username.
        ///
        /// After a successful update the auth cookie is refreshed so the navbar
        /// immediately shows the new name (the old claim would otherwise persist).
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUsername(
            [Bind(Prefix = "ChangeUsername")] ChangeUsernameViewModel model)
        {
            // The [Bind(Prefix = ...)] attribute tells the model binder to look for "ChangeUsername.Username"
            // in the form data, not just "Username". This allows one page to host several sub-forms
            // without their field names colliding.

            if (!ModelState.IsValid)
                return await ProfileWithErrorsAsync(vm => vm.ChangeUsername = model);

            var user = await _userService.UpdateUsernameAsync(CurrentUserId, model.Username.Trim());
            if (user == null) return await SignOutAndRedirectAsync();

            // Re-issue the auth cookie so the claim type "Name" (shown in the navbar) is up to date.
            await SignInUserAsync(user.Id, user.Username, user.Email);

            TempData["ProfileSuccess"] = "Username updated.";
            return RedirectToAction(nameof(Profile));
        }

        /// <summary>
        /// POST /Account/Profile/ChangePassword — verifies the current password and sets a new one.
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            [Bind(Prefix = "ChangePassword")] ChangePasswordViewModel model)
        {
            // Business rule: the new password must differ from the current one.
            // (Checked here because the ViewModel has no way to compare against the DB value.)
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

        /// <summary>
        /// POST /Account/Profile/ChangeCurrency — changes the MAIN wallet's currency.
        ///
        /// Delegates the actual conversion to IWalletService, which:
        /// - fetches the live exchange rate (adapter over the Frankfurter API),
        /// - converts the balance and every transaction of that wallet,
        /// - or just switches the label if the wallet is empty.
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeCurrency(
            [Bind(Prefix = "ChangeCurrency")] ChangeCurrencyViewModel model)
        {
            // Reject currencies that are not in the supported list (defence against tampered requests).
            if (!string.IsNullOrEmpty(model.Currency) && CurrencyInfo.Find(model.Currency) == null)
                ModelState.AddModelError("ChangeCurrency.Currency", "Unsupported currency.");

            if (!ModelState.IsValid)
                return await ProfileWithErrorsAsync(vm => vm.ChangeCurrency = model);

            // The profile page only changes the MAIN wallet, not all of them.
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

            // A context-aware message: nothing was converted if the wallet was empty.
            var rate = result.Rate.ToString("0.######", CultureInfo.InvariantCulture);
            TempData["ProfileSuccess"] =
                result.FromCode == result.ToCode
                    ? "Currency updated."
                : !result.Converted
                    ? $"Main wallet currency changed to {result.ToCode}. The wallet had no money or transactions, so nothing was converted."
                    : $"Main wallet converted to {result.ToCode} at 1 {result.FromCode} = {rate} {result.ToCode}. Your other wallets were not changed.";

            return RedirectToAction(nameof(Profile));
        }

        /// <summary>
        /// GET /Account/ExchangeRate?to=USD — live preview used by the currency picker.
        ///
        /// Returns JSON with the current and target rate plus the converted balance.
        /// The conversion is NOT saved — this is only for previewing in the UI.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ExchangeRate(string to)
        {
            var wallet = await _walletService.GetMainWalletAsync(CurrentUserId);
            if (wallet == null) return BadRequest(new { error = "You don't have a wallet yet." });

            var p = await _walletService.PreviewCurrencyChangeAsync(CurrentUserId, wallet.Id, to);
            if (!p.Success) return BadRequest(new { error = p.Error });

            // Anonymous object → JSON. The front-end uses these values to show "1 EUR ≈ 1.08 USD".
            return Json(new { from = p.FromCode, to = p.ToCode, rate = p.Rate, before = p.Before, after = p.After });
        }

        /// <summary>
        /// POST /Account/UploadPhoto — replaces the user's profile picture.
        ///
        /// The file is validated and stored by IProfilePhotoService (image type is detected from
        /// the file's magic bytes, not from its extension).
        /// </summary>
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

            // The service returns an error message (or null on success).
            var error = await _photos.SaveAsync(CurrentUserId, photo);
            if (error != null)
            {
                ModelState.AddModelError("Photo", error);
                return await ProfileWithErrorsAsync(_ => { });
            }

            TempData["ProfileSuccess"] = "Profile photo updated.";
            return RedirectToAction(nameof(Profile));
        }

        /// <summary>
        /// POST /Account/RemovePhoto — deletes the user's profile picture file.
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemovePhoto()
        {
            _photos.Delete(CurrentUserId);
            TempData["ProfileSuccess"] = "Profile photo removed.";
            return RedirectToAction(nameof(Profile));
        }

        /// <summary>
        /// POST /Account/DeleteAccount — permanently removes the user and all their data.
        ///
        /// Confirmation via password prevents accidental or CSRF-driven deletions.
        /// Cascade delete (configured in ApplicationDbContext) removes wallets and transactions.
        /// </summary>
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

            // Remove the physical profile photo file — the DB row is already gone.
            _photos.Delete(userId);
            return await SignOutAndRedirectAsync();
        }

        // ---------- helpers ----------

        /// <summary>
        /// Builds the ProfileViewModel from the current user + their main wallet.
        /// Returns null when the user no longer exists (which triggers a forced sign-out).
        /// </summary>
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
                // Each sub-form is pre-populated with the current value.
                ChangeUsername = new ChangeUsernameViewModel { Username = user.Username },
                ChangeCurrency = new ChangeCurrencyViewModel { Currency = code ?? string.Empty }
            };
        }

        /// <summary>
        /// Rebuilds the profile view model and applies the posted sub-form's data.
        /// Used when a sub-form has validation errors, so the page can show the errors
        /// next to the correct form without losing the other forms' current values.
        /// </summary>
        private async Task<IActionResult> ProfileWithErrorsAsync(Action<ProfileViewModel> applyPostedSection)
        {
            var vm = await BuildProfileAsync();
            if (vm == null) return await SignOutAndRedirectAsync();

            applyPostedSection(vm); // copy the submitted values back into the matching sub-form
            return View("Profile", vm);
        }

        /// <summary>
        /// Clears the auth cookie and redirects to the login page.
        /// Used when the user record no longer exists or after account deletion.
        /// </summary>
        private async Task<IActionResult> SignOutAndRedirectAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        /// <summary>
        /// Issues a new auth cookie for the given user.
        /// Called after a username change so the "Name" claim in the navbar is refreshed.
        /// </summary>
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
