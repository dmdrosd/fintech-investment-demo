namespace Fintech.Web.Models;

public sealed record AuditChainStatusResponse(
    bool IsValid,
    int EntriesChecked,
    string HeadHash,
    string? BrokenAt,
    DateTimeOffset? LastCreatedAt);
