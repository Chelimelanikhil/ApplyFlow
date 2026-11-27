
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
            (
                UserId,
                TokenHash,
                ExpiresAt
            )
            VALUES
            (
                @UserId,
                @TokenHash,
                @ExpiresAt
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt
        });
    }
}
