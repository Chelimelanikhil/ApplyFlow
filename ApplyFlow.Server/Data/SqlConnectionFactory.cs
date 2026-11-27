using Microsoft.Data.SqlClient;
using System.Data;

namespace ApplyFlow.Server.Data
{

    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }

    public sealed class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection is missing.");
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
