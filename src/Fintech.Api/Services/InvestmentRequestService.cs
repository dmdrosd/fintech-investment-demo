using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fintech.Api.Auth;
using Fintech.Api.Contracts;
using Fintech.Api.Data;
using Fintech.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Api.Services;

public sealed class InvestmentRequestService(InvestmentDbContext db)
{
    private const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

    public async Task<IReadOnlyList<InvestmentRequestResponse>> GetLatestAsync(CancellationToken cancellationToken)
    {
        return await db.InvestmentRequests
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => x.ToResponse())
            .ToListAsync(cancellationToken);
    }

    public async Task<InvestmentRequestResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.InvestmentRequests
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.ToResponse())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<InvestmentRequestResponse> CreateAsync(
        CreateInvestmentRequestDto dto,
        string idempotencyKey,
        UserContext user,
        CancellationToken cancellationToken)
    {
        idempotencyKey = NormalizeIdempotencyKey(idempotencyKey);

        var existing = await db.InvestmentRequests
            .AsNoTracking()
            .Where(x => x.IdempotencyKey == idempotencyKey)
            .Select(x => x.ToResponse())
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var request = new InvestmentRequest
        {
            ClientName = dto.ClientName.Trim(),
            Instrument = dto.Instrument.Trim().ToUpperInvariant(),
            Amount = dto.Amount,
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            OperationType = dto.OperationType,
            Status = RequestStatus.PendingCompliance,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = user.UserName,
            CreatedByUserId = user.UserId,
            IdempotencyKey = idempotencyKey
        };

        db.InvestmentRequests.Add(request);
        db.AuditLogs.Add(await AuditAsync("investment-request-created", request.Id, user, new
        {
            request.ClientName,
            request.Instrument,
            request.Amount,
            request.Currency,
            request.OperationType,
            request.IdempotencyKey
        }, cancellationToken));

        await db.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return request.ToResponse();
    }

    public async Task<InvestmentRequestResponse> UpdateStatusAsync(
        Guid id,
        UpdateInvestmentStatusDto dto,
        UserContext user,
        CancellationToken cancellationToken)
    {
        var request = await db.InvestmentRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null)
        {
            throw new KeyNotFoundException("Investment request was not found.");
        }

        if (request.Version != dto.ExpectedVersion)
        {
            throw new ConflictException($"Request was changed. Current version is {request.Version}.");
        }

        var oldStatus = request.Status;
        request.Status = dto.Status;
        request.Version++;

        db.AuditLogs.Add(await AuditAsync("investment-request-status-changed", request.Id, user, new
        {
            OldStatus = oldStatus,
            NewStatus = request.Status,
            request.Version
        }, cancellationToken));

        await db.SaveChangesAsync(cancellationToken);
        return request.ToResponse();
    }

    public async Task<IReadOnlyList<AuditLog>> GetAuditAsync(Guid? entityId, CancellationToken cancellationToken)
    {
        var query = db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).AsQueryable();
        if (entityId is not null)
        {
            query = query.Where(x => x.EntityId == entityId);
        }

        return await query.Take(100).ToListAsync(cancellationToken);
    }

    public async Task<AuditChainStatusResponse> VerifyAuditChainAsync(CancellationToken cancellationToken)
    {
        await BackfillLegacyAuditHashesAsync(cancellationToken);

        var logs = await db.AuditLogs
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var expectedPreviousHash = GenesisHash;
        foreach (var log in logs)
        {
            var payloadHash = ComputePayloadHash(log);
            var currentHash = ComputeCurrentHash(log.PreviousHash, payloadHash);
            if (!FixedTimeEquals(log.PreviousHash, expectedPreviousHash)
                || !FixedTimeEquals(log.PayloadHash, payloadHash)
                || !FixedTimeEquals(log.CurrentHash, currentHash))
            {
                return new AuditChainStatusResponse(
                    false,
                    logs.Count,
                    expectedPreviousHash,
                    log.Id.ToString("N"),
                    log.CreatedAt);
            }

            expectedPreviousHash = log.CurrentHash;
        }

        return new AuditChainStatusResponse(
            true,
            logs.Count,
            logs.Count == 0 ? GenesisHash : expectedPreviousHash,
            null,
            logs.LastOrDefault()?.CreatedAt);
    }

    private static string NormalizeIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency-Key header is required.", nameof(idempotencyKey));
        }

        return idempotencyKey.Trim();
    }

    private async Task<AuditLog> AuditAsync(
        string action,
        Guid entityId,
        UserContext user,
        object metadata,
        CancellationToken cancellationToken)
    {
        var audit = new AuditLog
        {
            ActorUserId = user.UserId,
            ActorUserName = user.UserName,
            Action = action,
            EntityType = "InvestmentRequest",
            EntityId = entityId,
            CreatedAt = DateTimeOffset.UtcNow,
            MetadataJson = JsonSerializer.Serialize(metadata)
        };

        audit.PreviousHash = await db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => x.CurrentHash)
            .FirstOrDefaultAsync(cancellationToken) ?? GenesisHash;
        audit.PayloadHash = ComputePayloadHash(audit);
        audit.CurrentHash = ComputeCurrentHash(audit.PreviousHash, audit.PayloadHash);

        return audit;
    }

    private static string ComputePayloadHash(AuditLog audit)
    {
        var payload = JsonSerializer.Serialize(new
        {
            audit.Id,
            audit.ActorUserId,
            audit.ActorUserName,
            audit.Action,
            audit.EntityType,
            audit.EntityId,
            audit.CreatedAt,
            audit.MetadataJson
        });

        return Sha256Hex(payload);
    }

    private static string ComputeCurrentHash(string previousHash, string payloadHash)
    {
        return Sha256Hex($"{previousHash}:{payloadHash}");
    }

    private static string Sha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private async Task BackfillLegacyAuditHashesAsync(CancellationToken cancellationToken)
    {
        var logs = await db.AuditLogs
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (!logs.Any(IsLegacyAuditWithoutHash))
        {
            return;
        }

        var previousHash = GenesisHash;
        foreach (var log in logs)
        {
            log.PreviousHash = previousHash;
            log.PayloadHash = ComputePayloadHash(log);
            log.CurrentHash = ComputeCurrentHash(log.PreviousHash, log.PayloadHash);
            previousHash = log.CurrentHash;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsLegacyAuditWithoutHash(AuditLog log)
    {
        return string.IsNullOrWhiteSpace(log.PreviousHash)
            && string.IsNullOrWhiteSpace(log.PayloadHash)
            && string.IsNullOrWhiteSpace(log.CurrentHash);
    }
}

public static class InvestmentRequestMapping
{
    public static InvestmentRequestResponse ToResponse(this InvestmentRequest request)
    {
        return new InvestmentRequestResponse(
            request.Id,
            request.ClientName,
            request.Instrument,
            request.Amount,
            request.Currency,
            request.OperationType,
            request.Status,
            request.CreatedAt,
            request.CreatedBy,
            request.IdempotencyKey,
            request.Version);
    }
}
