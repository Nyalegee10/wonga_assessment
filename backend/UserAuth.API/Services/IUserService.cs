using UserAuth.API.Models.DTOs;

namespace UserAuth.API.Services;

public interface IUserService
{
    Task<UserDetailsDto?> GetUserByIdAsync(int id);
}
