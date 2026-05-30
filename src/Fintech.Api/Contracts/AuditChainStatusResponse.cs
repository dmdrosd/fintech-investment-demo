namespace Fintech.Api.Contracts;

public sealed record AuditChainStatusResponse(
    bool IsValid,
    int EntriesChecked,
    string HeadHash,
    string? BrokenAt,
    DateTimeOffset? LastCreatedAt);
