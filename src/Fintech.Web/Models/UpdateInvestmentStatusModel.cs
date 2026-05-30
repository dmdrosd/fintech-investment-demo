namespace Fintech.Web.Models;

public sealed record UpdateInvestmentStatusModel(RequestStatus Status, int ExpectedVersion);
