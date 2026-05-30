using Fintech.Api.Domain;

namespace Fintech.Api.Contracts;

public sealed class UpdateInvestmentStatusDto
{
    public RequestStatus Status { get; set; }
    public int ExpectedVersion { get; set; }
}
