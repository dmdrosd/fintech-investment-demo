using System.ComponentModel.DataAnnotations;
using Fintech.Api.Domain;

namespace Fintech.Api.Contracts;

public sealed class CreateInvestmentRequestDto
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string ClientName { get; set; } = string.Empty;

    [Required, StringLength(32)]
    public string Instrument { get; set; } = string.Empty;

    [Range(1, 50_000_000)]
    public decimal Amount { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "RUB";

    public OperationType OperationType { get; set; } = OperationType.Buy;
}
