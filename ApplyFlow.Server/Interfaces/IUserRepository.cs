
using ApplyFlow.Server.Models;

namespace ApplyFlow.Server.Interfaces;

public interface IUserRepository
{
    Task<int> CreateAsync(User user);

    Task<User?> GetByEmailAsync(string normalizedEmail);

    Task<User?> GetByIdAsync(int userId);
}
