
namespace ApplyFlow.Server.Interfaces;

public interface IRefreshTokenRepository
{
    Task SaveAsync(
        int userId,
        string tokenHash,
        DateTime expiresAt);

    Task<int?> RotateAsync(
        string oldTokenHash,
        string newTokenHash,
        DateTime newExpiresAt);

    Task RevokeAsync(string tokenHash);
}