using ApplyFlow.Server.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace ApplyFlow.Server.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HealthController(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            application = "ApplyFlow API"
        });
    }

    [HttpGet("database")]
    public async Task<IActionResult> Database()
    {
        try
        {
            using var connection =
                _connectionFactory.CreateConnection();

            var result = await connection
                .ExecuteScalarAsync<int>("SELECT 1");

            return Ok(new
            {
                status = "Connected",
                database = "ApplyFlowDB",
                dapperTest = result == 1
            });
        }
        catch (Exception)
        {
            return StatusCode(503, new
            {
                status = "DatabaseUnavailable",
                message = "Unable to connect to SQL Server."
            });
        }
    }
}
