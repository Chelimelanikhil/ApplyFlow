
using ApplyFlow.Server.DTOs;

namespace ApplyFlow.Server.Interfaces;

public interface IAuthService
{
    Task<int?> RegisterAsync(RegisterRequest request);

    Task<AuthResponse?> LoginAsync(LoginRequest request);
}
