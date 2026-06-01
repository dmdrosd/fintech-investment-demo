using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fintech.Web.Models;

namespace Fintech.Web.Api;

public sealed class InvestmentApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<InvestmentRequestResponse>> GetLatestAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<InvestmentRequestResponse>>(
            "api/investment-requests",
            JsonOptions,
            cancellationToken) ?? [];
    }

    public async Task<AuditChainStatusResponse?> GetAuditChainStatusAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<AuditChainStatusResponse>(
            "api/audit/chain/status",
            JsonOptions,
            cancellationToken);
    }

    public async Task<ApiResult<InvestmentRequestResponse>> CreateAsync(
        CreateInvestmentRequestModel model,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/investment-requests")
        {
            Content = JsonContent.Create(model, options: JsonOptions)
        };

        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var payload = await response.Content.ReadFromJsonAsync<InvestmentRequestResponse>(JsonOptions, cancellationToken);
            return ApiResult<InvestmentRequestResponse>.Success(payload!);
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return ApiResult<InvestmentRequestResponse>.Failure((int)response.StatusCode, error);
    }

    public async Task<ApiResult<InvestmentRequestResponse>> UpdateStatusAsync(
        InvestmentRequestResponse request,
        RequestStatus status,
        CancellationToken cancellationToken)
    {
        var body = new UpdateInvestmentStatusModel(status, request.Version);
        using var response = await httpClient.PutAsJsonAsync(
            $"api/investment-requests/{request.Id}/status",
            body,
            JsonOptions,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<InvestmentRequestResponse>(JsonOptions, cancellationToken);
            return ApiResult<InvestmentRequestResponse>.Success(payload!);
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return ApiResult<InvestmentRequestResponse>.Failure((int)response.StatusCode, error);
    }
}
