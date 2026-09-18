using UserService.DTOs;

namespace UserService.Services
{
    public interface IUserService
    {
        Task<UserResponseDto> RegisterAsync(RegisterRequestDto request);

        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

        Task<UserResponseDto?> GetByIdAsync(int id);

        Task<UserResponseDto> UpdateAsync(int id, UpdateRequestDto request);

        Task DeleteAsync(int id);
    }
}
