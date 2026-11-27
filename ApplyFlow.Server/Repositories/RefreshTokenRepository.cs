
using ApplyFlow.Server.Data;
using ApplyFlow.Server.Interfaces;
using Dapper;

namespace ApplyFlow.Server.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
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
            INSERT INTO dbo.RefreshTokens
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

    public async Task<int?> RotateAsync(
        string oldTokenHash,
        string newTokenHash,
        DateTime newExpiresAt)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            const string revokeSql = """
                UPDATE dbo.RefreshTokens
                SET RevokedAt = SYSUTCDATETIME()
                OUTPUT INSERTED.UserId
                WHERE TokenHash = @OldTokenHash
                  AND RevokedAt IS NULL
                  AND ExpiresAt > SYSUTCDATETIME();
                """;

            var userId = await connection.QuerySingleOrDefaultAsync<int?>(
                revokeSql,
                new { OldTokenHash = oldTokenHash },
                transaction);

            if (userId is null)
            {
                transaction.Rollback();
                return null;
            }

            const string insertSql = """
                INSERT INTO dbo.RefreshTokens
                    (UserId, TokenHash, ExpiresAt)
                VALUES
                    (@UserId, @NewTokenHash, @NewExpiresAt);
                """;

            await connection.ExecuteAsync(insertSql, new
            {
                UserId = userId.Value,
                NewTokenHash = newTokenHash,
                NewExpiresAt = newExpiresAt
            }, transaction);

            transaction.Commit();

            return userId.Value;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task RevokeAsync(string tokenHash)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE dbo.RefreshTokens
            SET RevokedAt = SYSUTCDATETIME()
            WHERE TokenHash = @TokenHash
              AND RevokedAt IS NULL;
            """;

        await connection.ExecuteAsync(sql, new { TokenHash = tokenHash });
    }
}