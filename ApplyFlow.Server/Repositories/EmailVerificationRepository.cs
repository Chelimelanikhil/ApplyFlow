
using ApplyFlow.Server.Data;
using ApplyFlow.Server.Interfaces;
using Dapper;

namespace ApplyFlow.Server.Repositories;

public sealed class EmailVerificationRepository
    : IEmailVerificationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EmailVerificationRepository(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(
        int userId,
        string tokenHash,
        DateTime expiresAt)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO EmailVerificationTokens
                (UserId, TokenHash, ExpiresAt)
            VALUES
                (@UserId, @TokenHash, @ExpiresAt);
            """;

        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt
        });
    }

    public async Task<int?> VerifyAsync(string tokenHash)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            const string tokenSql = """
                UPDATE dbo.EmailVerificationTokens
                SET UsedAt = SYSUTCDATETIME()
                OUTPUT INSERTED.UserId
                WHERE TokenHash = @TokenHash
                  AND UsedAt IS NULL
                  AND ExpiresAt > SYSUTCDATETIME();
                """;

            var userId = await connection.QuerySingleOrDefaultAsync<int?>(
                tokenSql,
                new { TokenHash = tokenHash },
                transaction);

            if (userId is null)
            {
                transaction.Rollback();
                return null;
            }

            const string userSql = """
                UPDATE dbo.Users
                SET IsEmailVerified = 1,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId;
                """;

            var updated = await connection.ExecuteAsync(
                userSql,
                new { UserId = userId.Value },
                transaction);

            if (updated != 1)
            {
                transaction.Rollback();
                return null;
            }

            transaction.Commit();

            return userId.Value;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
