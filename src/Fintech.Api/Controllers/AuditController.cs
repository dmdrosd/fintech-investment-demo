using Fintech.Api.Auth;
using Fintech.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fintech.Api.Controllers;

[ApiController]
[Route("api/audit")]
public sealed class AuditController(InvestmentRequestService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Auditor)]
    public async Task<IActionResult> Get([FromQuery] Guid? entityId, CancellationToken cancellationToken)
    {
        return Ok(await service.GetAuditAsync(entityId, cancellationToken));
    }

    [HttpGet("chain/status")]
    [Authorize(Policy = Policies.Auditor)]
    public async Task<IActionResult> GetChainStatus(CancellationToken cancellationToken)
    {
        return Ok(await service.VerifyAuditChainAsync(cancellationToken));
    }
}
