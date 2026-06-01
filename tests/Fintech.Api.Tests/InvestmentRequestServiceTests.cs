using Fintech.Api.Auth;
using Fintech.Api.Contracts;
using Fintech.Api.Data;
using Fintech.Api.Domain;
using Fintech.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Api.Tests;

public sealed class InvestmentRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsExistingRequest_WhenIdempotencyKeyIsRepeated()
    {
        await using var db = CreateDbContext();
        var service = new InvestmentRequestService(db);
        var command = new CreateInvestmentRequestDto
        {
            ClientName = "Demo Client",
            Instrument = "BANK",
            Amount = 1000,
            Currency = "RUB",
            OperationType = OperationType.Buy
        };

        var first = await service.CreateAsync(command, "same-key", User(), CancellationToken.None);
        var second = await service.CreateAsync(command, "same-key", User(), CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await db.InvestmentRequests.CountAsync());
        Assert.Equal(1, await db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task UpdateStatusAsync_ThrowsConflict_WhenExpectedVersionIsStale()
    {
        await using var db = CreateDbContext();
        var service = new InvestmentRequestService(db);
        var created = await service.CreateAsync(new CreateInvestmentRequestDto
        {
            ClientName = "Demo Client",
            Instrument = "SBER",
            Amount = 2000,
            Currency = "RUB",
            OperationType = OperationType.Sell
        }, "status-key", User(), CancellationToken.None);

        await service.UpdateStatusAsync(
            created.Id,
            new UpdateInvestmentStatusDto { Status = RequestStatus.Approved, ExpectedVersion = created.Version },
            User(),
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateStatusAsync(
            created.Id,
            new UpdateInvestmentStatusDto { Status = RequestStatus.Executed, ExpectedVersion = created.Version },
            User(),
            CancellationToken.None));
    }

    [Fact]
    public async Task VerifyAuditChainAsync_ReturnsValidChain_AfterCreateAndStatusChange()
    {
        await using var db = CreateDbContext();
        var service = new InvestmentRequestService(db);

        var created = await service.CreateAsync(new CreateInvestmentRequestDto
        {
            ClientName = "Demo Client",
            Instrument = "BTC-ETF",
            Amount = 3000,
            Currency = "USD",
            OperationType = OperationType.Buy
        }, "chain-key", User(), CancellationToken.None);

        await service.UpdateStatusAsync(
            created.Id,
            new UpdateInvestmentStatusDto { Status = RequestStatus.Approved, ExpectedVersion = created.Version },
            User(),
            CancellationToken.None);

        var chain = await service.VerifyAuditChainAsync(CancellationToken.None);

        Assert.True(chain.IsValid);
        Assert.Equal(2, chain.EntriesChecked);
        Assert.Equal(64, chain.HeadHash.Length);
    }

    [Fact]
    public async Task VerifyAuditChainAsync_StaysValid_WhenTimestampsTruncatedToMicroseconds()
    {
        await using var db = CreateDbContext();
        var service = new InvestmentRequestService(db);

        await service.CreateAsync(new CreateInvestmentRequestDto
        {
            ClientName = "Demo Client",
            Instrument = "SBER",
            Amount = 1234,
            Currency = "RUB",
            OperationType = OperationType.Buy
        }, "pg-roundtrip-key", User(), CancellationToken.None);

        // Имитируем round-trip через PostgreSQL timestamptz: усечь CreatedAt до микросекунд.
        // С фиксом время уже усечено при записи -> это no-op и цепочка валидна.
        // Без фикса хэш считался от 100-нс времени -> после усечения пересчёт разойдётся -> broken.
        foreach (var log in db.AuditLogs)
        {
            log.CreatedAt = new DateTimeOffset(log.CreatedAt.Ticks - log.CreatedAt.Ticks % 10, log.CreatedAt.Offset);
        }

        await db.SaveChangesAsync();

        var chain = await service.VerifyAuditChainAsync(CancellationToken.None);

        Assert.True(chain.IsValid);
    }

    private static InvestmentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InvestmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new InvestmentDbContext(options);
    }

    private static UserContext User() => new("operator-1", "Operator One");
}
