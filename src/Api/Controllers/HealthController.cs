using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static readonly string _assemblyVersion =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var dbConnected = await db.Database.CanConnectAsync(ct);

        return Ok(new
        {
            status = "OK",
            version = _assemblyVersion,
            environment = env.EnvironmentName,
            dbConnected
        });
    }
}
