using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Host.Options;
using Host.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Host.Authentication
{
    public sealed class DatabaseBasedAuthService : IAuthService
    {
        private readonly AuthDbContext _context;
        private readonly JwtOptions _options;

        public DatabaseBasedAuthService(AuthDbContext context, IOptionsSnapshot<JwtOptions> options)
        {
            _context = context;
            _options = options.Value;
        }
        
        public async ValueTask<ApplicationUser> AuthenticateUserAsync(string username, string password, CancellationToken ct)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username, ct);
            
            ct.ThrowIfCancellationRequested();

            if (user == null || user.PasswordHash != Encrypt(password, ReadSaltFromBase64(user.Salt)))
            {
                return null;
            }

            return user;
        }

        public string GenerateJwtToken(ApplicationUser userInfo)
        {
            var secretKey = _options.SecretKey;
            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
                new Claim("role", userInfo.Role)
            };
            var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims,
                expires: DateTime.Now.AddMinutes(30d), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        private static byte[] ReadSaltFromBase64(string saltBase64)
        {
            return Convert.FromBase64String(saltBase64);
        }
        
        private static string Encrypt(string password, byte[] salt)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10_000);
            byte[] hash = pbkdf2.GetBytes(20);
            
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            return Convert.ToBase64String(hashBytes);
        }
    }
}