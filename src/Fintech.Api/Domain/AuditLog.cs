namespace Fintech.Api.Domain;

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorUserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string MetadataJson { get; set; } = "{}";
    public string PreviousHash { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string CurrentHash { get; set; } = string.Empty;
}
