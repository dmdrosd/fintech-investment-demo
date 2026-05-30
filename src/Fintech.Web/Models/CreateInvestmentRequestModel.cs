using System.ComponentModel.DataAnnotations;

namespace Fintech.Web.Models;

public sealed class CreateInvestmentRequestModel
{
    [Required(ErrorMessage = "Укажите клиента")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Имя клиента должно быть от 2 до 120 символов")]
    public string ClientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите инструмент")]
    public string Instrument { get; set; } = "BANK";

    [Range(1, 50_000_000, ErrorMessage = "Сумма должна быть от 1 до 50 000 000")]
    public decimal Amount { get; set; } = 100_000;

    [Required]
    public string Currency { get; set; } = "RUB";

    public OperationType OperationType { get; set; } = OperationType.Buy;
}
