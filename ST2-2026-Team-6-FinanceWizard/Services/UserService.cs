using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    /// <summary>
    /// User account management: registration, validation, profile changes, deletion.
    ///
    /// Design notes:
    /// - Passwords are hashed with BCrypt (deliberately slow, salted per hash), never stored in plaintext.
    /// - Passwords are never compared by direct string equality — only BCrypt.Verify.
    /// - Registration creates a default wallet (EUR, balance 0) for the new user, so the
    ///   dashboard / transaction pages have something to attach transactions to.
    /// - The "GenerateJwtToken" method is present (legacy endpoint) but the Cookie scheme is
    ///   what the AccountController actually uses. The JWT method is kept for potential
    ///   future use / external API integration.
    /// - Account deletion relies on cascade delete (User → Wallets → Transactions), configured
    ///   in ApplicationDbContext.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IEfRepository<User> _userRepo;
        private readonly IEfRepository<Wallet> _walletRepo;
        private readonly IConfiguration _config;

        public UserService(
            IEfRepository<User> userRepo,
            IEfRepository<Wallet> walletRepo,
            IEfRepository<Transaction> transactionRepo,
            IExchangeRateService rates,
            IConfiguration config)
        {
            _userRepo = userRepo;
            _walletRepo = walletRepo;
            _config = config;
        }

        /// <summary>
        /// Registers a new user and creates their first (empty EUR) wallet.
        /// Throws when the email is already registered.
        /// </summary>
        public async Task<User> RegisterAsync(string username, string email, string password)
        {
            // Email must be unique — checked here, and protected at the DB level by the email column.
            var existing = await _userRepo.All().FirstOrDefaultAsync(u => u.Email == email);
            if (existing != null) throw new Exception("User with this email already exists.");

            // BCrypt generates a fresh salt for every hash, so two users with the same password
            // get different hashes. That defeats rainbow-table attacks.
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync(); // save first to get the auto-generated user.Id

            // A brand-new user needs a wallet, otherwise they cannot add a transaction.
            var wallet = new Wallet { UserId = user.Id, Balance = 0, Currency = "EUR" };
            await _walletRepo.AddAsync(wallet);
            await _walletRepo.SaveChangesAsync();

            user.Wallets.Add(wallet);
            return user;
        }

        /// <summary>
        /// Legacy JWT login. Returns a JWT string on success, throws on failure.
        /// The current AccountController uses cookie auth instead; this is kept for
        /// potential external API usage.
        /// </summary>
        public async Task<string> LoginAsync(string email, string password)
        {
            var user = await _userRepo.All().FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                throw new Exception("Invalid email or password.");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isPasswordValid)
            {
                throw new Exception("Invalid email or password.");
            }

            var token = GenerateJwtToken(user);
            return token;
        }

        /// <summary>
        /// Loads a user (with their wallets) by id. Returns null if not found.
        /// </summary>
        public async Task<User?> GetByIdAsync(long userId)
        {
            var user = await _userRepo.All()
                .Include(u => u.Wallets)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user;
        }

        /// <summary>
        /// Verifies email + password and returns the user on success, or null on failure.
        /// Used by AccountController.Login to decide whether to sign the user in.
        /// </summary>
        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            var user = await _userRepo.All().FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;

            // BCrypt.Verify is constant-time-ish and uses the salt stored inside the hash.
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isPasswordValid) return null;

            return user;
        }

        /// <summary>
        /// Builds a JWT token for the given user (7-day expiry).
        /// Only used by the legacy LoginAsync method; the app uses cookies for browser sessions.
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var keyString = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(keyString)) throw new Exception("JWT Key is missing in appsettings.json");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Updates the user's display name. Returns the updated user, or null if the user was not found.
        /// </summary>
        public async Task<User?> UpdateUsernameAsync(long userId, string newUsername)
        {
            var user = await _userRepo.All().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            user.Username = newUsername;
            await _userRepo.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Verifies the current password and stores a new hash.
        /// Returns false if the user is not found or the current password is wrong.
        /// </summary>
        public async Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword)
        {
            var user = await _userRepo.All().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            // Текущата парола се проверява с BCrypt.Verify
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepo.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Deletes the user after verifying their password.
        /// Cascade delete (configured in ApplicationDbContext) removes their wallets
        /// and all transactions on those wallets.
        /// </summary>
        public async Task<bool> DeleteAccountAsync(long userId, string password)
        {
            var user = await _userRepo.All().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return false;

            // Каскадното изтриване (User -> Wallet -> Transactions) е настроено в ApplicationDbContext/миграцията
            _userRepo.Delete(user);
            await _userRepo.SaveChangesAsync();
            return true;
        }
    }
}
