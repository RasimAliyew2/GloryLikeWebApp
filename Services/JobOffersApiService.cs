using System.Text.Json;
using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public sealed class JobOffersApiService : IJobOffersApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;

    public JobOffersApiService(HttpClient httpClient)
    {
        _httpClient = httpClient
            ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<IReadOnlyList<JobOfferApiItem>> GetJobOffersAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            "api/JobOffers",
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"JobOffers API error. HTTP {(int)response.StatusCode}. {ExtractMessage(body)}");
        }

        try
        {
            return JsonSerializer.Deserialize<List<JobOfferApiItem>>(
                       body,
                       JsonOptions)
                   ?? new List<JobOfferApiItem>();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "JobOffers API JSON cavabı uyğun formatda deyil.",
                ex);
        }
    }

    private static string ExtractMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty(
                    "message",
                    out var message))
            {
                return message.GetString() ?? string.Empty;
            }

            if (document.RootElement.TryGetProperty(
                    "title",
                    out var title))
            {
                return title.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // Backend may return plain text instead of JSON.
        }

        return body.Trim();
    }
}
