
using ApplyFlow.Server.DTOs;

namespace ApplyFlow.Server.Interfaces;

public interface IAuthService
{
    Task<int?> RegisterAsync(RegisterRequest request);

    Task<AuthResponse?> LoginAsync(LoginRequest request);

    Task<AuthResponse?> RefreshAsync(string refreshToken);

    Task LogoutAsync(string refreshToken);

    Task<bool> VerifyEmailAsync(string token);
}