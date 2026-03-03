using UserAuth.Application.DTOs;

namespace UserAuth.Application.Interfaces;

public interface IUserService
{
    Task<UserDetailsDto?> GetUserByIdAsync(int id);
}
