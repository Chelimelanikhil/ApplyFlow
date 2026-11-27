
namespace ApplyFlow.Server.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(
        int userId,
        string fullName,
        string email,
        string role);

    (string Token, DateTime ExpiresAt) GenerateRefreshToken();

    string HashToken(string token);
}
