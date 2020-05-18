using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Host.Authentication
{
    public sealed class LocalAuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        
        private readonly List<ApplicationUser> _users = new List<ApplicationUser>(2);

        public LocalAuthService(IConfiguration configuration)
        {
            var salt1 = GenerateSalt(out var saltValue1);
            var salt2 = GenerateSalt(out var saltValue2);
            _users.Add(new ApplicationUser
            {
                Id = 1,
                PasswordHash = Encrypt("test_password", salt1),
                Salt = saltValue1,
                Role = Roles.Admin,
                Username = "test"
            });
            _users.Add(new ApplicationUser
            {
                Id = 2,
                PasswordHash = Encrypt("ro_password", salt2),
                Salt = saltValue2,
                Role = Roles.ReadOnly,
                Username = "ro"
            });
            _configuration = configuration;
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

        private static byte[] GenerateSalt(out string saltBase64)
        {
            byte[] salt = new byte[16];
            using var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(salt);
            saltBase64 = Convert.ToBase64String(salt);
            return salt;
        }

        private static byte[] ReadSaltFromBase64(string saltBase64)
        {
            return Convert.FromBase64String(saltBase64);
        }
        
        public ValueTask<ApplicationUser> AuthenticateUserAsync(string username, string password, CancellationToken ct)
        {
            var user = _users.FirstOrDefault(u => u.Username == username);
            
            if (user == null || user.PasswordHash != Encrypt(password, ReadSaltFromBase64(user.Salt)))
                return new ValueTask<ApplicationUser>((ApplicationUser) null);
            
            return new ValueTask<ApplicationUser>(user);
        }

        public string GenerateJwtToken(ApplicationUser userInfo)
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
                new Claim("role", userInfo.Role)
            };
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"], claims,
                expires: DateTime.Now.AddMinutes(30d), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}