namespace Fintech.Api.Domain;

public sealed class InvestmentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientName { get; set; } = string.Empty;
    public string Instrument { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public OperationType OperationType { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
}
