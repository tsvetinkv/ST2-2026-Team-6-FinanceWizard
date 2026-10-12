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

            var wallet = new Wallet { UserId = user.Id, Name = "Main account", Type = WalletTypes.Spending, Balance = 0, Currency = "EUR" };
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
    }
}
