using Host.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Host.Services
{
    public sealed class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions options) : base(options)
        {
        }
        
        public DbSet<ApplicationUser> Users { get; set; }
    }
}