using Fintech.Api.Auth;
using Fintech.Api.Contracts;
using Fintech.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fintech.Api.Controllers;

[ApiController]
[Route("api/investment-requests")]
public sealed class InvestmentRequestsController(InvestmentRequestService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Operator)]
    public async Task<IActionResult> GetLatest(CancellationToken cancellationToken)
    {
        return Ok(await service.GetLatestAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Operator)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Operator)]
    public async Task<IActionResult> Create(
        [FromBody] CreateInvestmentRequestDto dto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Idempotency key is required",
                Detail = "Send Idempotency-Key header to make request retries safe.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await service.CreateAsync(dto, idempotencyKey, UserContext.From(User), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Policies.Operator)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateInvestmentStatusDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.UpdateStatusAsync(id, dto, UserContext.From(User), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Concurrency conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
