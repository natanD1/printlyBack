using Microsoft.AspNetCore.Mvc;
using Printly.Infrastructure.Persistence;

namespace Printly.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly PrintlyDbContext _dbContext;

    public HealthController(PrintlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("database")]
    public async Task<IActionResult> CheckDatabase()
    {
        var canConnect = await _dbContext.Database.CanConnectAsync();

        return canConnect
            ? Ok(new { status = "ok", message = "Conexao com o banco de dados estabelecida com sucesso." })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "error", message = "Nao foi possivel conectar ao banco de dados." });
    }
}
