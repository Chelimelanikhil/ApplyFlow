
using ApplyFlow.Server.Data;
using ApplyFlow.Server.Interfaces;
using ApplyFlow.Server.Models;
using Dapper;

namespace ApplyFlow.Server.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            const string userSql = """
                INSERT INTO dbo.Users
                (
                    FullName,
                    Email,
                    NormalizedEmail,
                    PasswordHash,
                    Role,
                    IsEmailVerified,
                    IsActive
                )
                OUTPUT INSERTED.UserId
                VALUES
                (
                    @FullName,
                    @Email,
                    @NormalizedEmail,
                    @PasswordHash,
                    @Role,
                    0,
                    1
                );
                """;

            // SQL Server generates and returns the new UserId.
            var userId = await connection.ExecuteScalarAsync<int>(
                userSql,
                user,
                transaction
            );

            const string profileSql = """
                INSERT INTO dbo.UserProfiles (UserId)
                VALUES (@UserId);
                """;

            await connection.ExecuteAsync(
                profileSql,
                new { UserId = userId },
                transaction
            );

            transaction.Commit();

            return userId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string normalizedEmail)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
        SELECT
            UserId,
            FullName,
            Email,
            NormalizedEmail,
            PasswordHash,
            Role,
            IsEmailVerified,
            IsActive
        FROM dbo.Users
        WHERE NormalizedEmail = @NormalizedEmail;
        """;

        return await connection.QuerySingleOrDefaultAsync<User>(
            sql,
            new { NormalizedEmail = normalizedEmail }
        );
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
        SELECT
            UserId,
            FullName,
            Email,
            NormalizedEmail,
            PasswordHash,
            Role,
            IsEmailVerified,
            IsActive
        FROM dbo.Users
        WHERE UserId = @UserId;
        """;

        return await connection.QuerySingleOrDefaultAsync<User>(
            sql,
            new { UserId = userId });
    }

}
