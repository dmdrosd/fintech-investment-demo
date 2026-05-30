using Fintech.Api.Domain;

namespace Fintech.Api.Contracts;

public sealed record InvestmentRequestResponse(
    Guid Id,
    string ClientName,
    string Instrument,
    decimal Amount,
    string Currency,
    OperationType OperationType,
    RequestStatus Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    string IdempotencyKey,
    int Version);
