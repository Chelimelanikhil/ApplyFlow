
namespace ApplyFlow.Server.Interfaces;

public interface IRefreshTokenRepository
{
    Task SaveAsync(
        int userId,
        string tokenHash,
        DateTime expiresAt);
}
