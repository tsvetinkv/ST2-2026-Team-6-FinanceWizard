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

        public async Task<User> RegisterAsync(string username, string email, string password)
        {
            var existing = await _userRepo.All().FirstOrDefaultAsync(u => u.Email == email);
            if (existing != null) throw new Exception("User with this email already exists.");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            var wallet = new Wallet { UserId = user.Id, Balance = 0, Currency = "EUR" };
            await _walletRepo.AddAsync(wallet);
            await _walletRepo.SaveChangesAsync();

            user.Wallets.Add(wallet);
            return user;
        }

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

        public async Task<User?> GetByIdAsync(long userId)
        {
            var user = await _userRepo.All()
                .Include(u => u.Wallets)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user;
        }

        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            var user = await _userRepo.All().FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isPasswordValid) return null;

            return user;
        }

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

        public async Task<User?> UpdateUsernameAsync(long userId, string newUsername)
        {
            var user = await _userRepo.All().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            user.Username = newUsername;
            await _userRepo.SaveChangesAsync();
            return user;
        }

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
