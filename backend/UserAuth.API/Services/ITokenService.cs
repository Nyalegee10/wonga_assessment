using UserAuth.API.Models;

namespace UserAuth.API.Services;

public interface ITokenService
{
    string GenerateToken(User user);
    int? ValidateTokenAndGetUserId(string token);
}
