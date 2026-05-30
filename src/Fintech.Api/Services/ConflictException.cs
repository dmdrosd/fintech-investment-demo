namespace Fintech.Api.Services;

public sealed class ConflictException(string message) : InvalidOperationException(message);
