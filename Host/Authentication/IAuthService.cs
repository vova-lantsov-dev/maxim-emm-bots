using System.Threading;
using System.Threading.Tasks;

namespace Host.Authentication
{
    public interface IAuthService
    {
        ValueTask<ApplicationUser> AuthenticateUserAsync(string username, string password, CancellationToken ct);

        string GenerateJwtToken(ApplicationUser userInfo);
    }
}