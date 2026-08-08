using Microsoft.AspNetCore.Mvc;
using PrintFlow.Infrastructure.Persistence;

namespace PrintFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly PrintFlowDbContext _dbContext;

    public HealthController(PrintFlowDbContext dbContext)
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
