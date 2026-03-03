using UserAuth.Domain.Common;
using UserAuth.Domain.Entities;

namespace UserAuth.Domain.Interfaces.Services;

public interface ITokenService
{
    TokenResult GenerateToken(User user);
    int? ValidateTokenAndGetUserId(string token);
}
