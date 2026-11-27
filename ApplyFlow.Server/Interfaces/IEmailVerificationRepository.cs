
namespace ApplyFlow.Server.Interfaces;

public interface IEmailVerificationRepository
{
    Task SaveAsync(
        int userId,
        string tokenHash,
        DateTime expiresAt);

    Task<int?> VerifyAsync(string tokenHash);
}
