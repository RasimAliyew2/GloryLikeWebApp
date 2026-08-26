using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;

namespace GloryLikeWebApp.Services;

public sealed class CompanyStructureApiService : ICompanyStructureApiService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly HttpClient _httpClient;
    private readonly ILogger<CompanyStructureApiService> _logger;

    public CompanyStructureApiService(
        HttpClient httpClient,
        ILogger<CompanyStructureApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<CompanyStructureApiResult> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/company/structure?actorUserId={actorUserId}"),
            cancellationToken);
    }

    public Task<CompanyStructureApiResult> SaveAsync(
        int actorUserId,
        SaveCompanyStructureInput input,
        CancellationToken cancellationToken = default)
    {
        var request = new BackendSaveCompanyStructureRequest
        {
            ActorUserId = actorUserId,
            Departments = input.Departments ?? new()
        };

        return SendAsync(
            new HttpRequestMessage(HttpMethod.Put, "api/company/structure")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            },
            cancellationToken);
    }

    public async Task<CompanyStructureApiResult> ImportAsync(
        int actorUserId,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(streamContent, "file", fileName);

        return await SendAsync(
            new HttpRequestMessage(
                HttpMethod.Post,
                $"api/company/structure/import?actorUserId={actorUserId}")
            {
                Content = form
            },
            cancellationToken);
    }

    public async Task<CompanyStructureFileResult> ExportAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/company/structure/export?actorUserId={actorUserId}",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var payload = await TryDeserializeAsync(response, cancellationToken);
                return new CompanyStructureFileResult
                {
                    Success = false,
                    Message = payload?.Message ?? "Company structure Excel could not be downloaded."
                };
            }

            return new CompanyStructureFileResult
            {
                Success = true,
                FileName = response.Content.Headers.ContentDisposition?.FileNameStar
                    ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                    ?? "BothFind_Template_OrgStructure.xlsx",
                Content = await response.Content.ReadAsByteArrayAsync(cancellationToken)
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new CompanyStructureFileResult
            {
                Success = false,
                Message = "Company structure Excel request timed out."
            };
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Company structure export failed.");
            return new CompanyStructureFileResult
            {
                Success = false,
                Message = "BackendApp could not be reached."
            };
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Company structure export returned invalid JSON.");
            return new CompanyStructureFileResult
            {
                Success = false,
                Message = "Backend returned an invalid response."
            };
        }
    }

    private async Task<CompanyStructureApiResult> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            using (request)
            using (var response = await _httpClient.SendAsync(request, cancellationToken))
            {
                var payload = await TryDeserializeAsync(response, cancellationToken);
                if (payload is not null)
                    return CompanyStructureApiResult.From(payload);

                return CompanyStructureApiResult.Fail(
                    response.IsSuccessStatusCode
                        ? "Backend returned an empty company structure response."
                        : "Company structure request failed.");
            }
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyStructureApiResult.Fail("Company structure request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Company structure API request failed.");
            return CompanyStructureApiResult.Fail("BackendApp could not be reached.");
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Company structure API returned invalid JSON.");
            return CompanyStructureApiResult.Fail("Backend returned an invalid response.");
        }
    }

    private static async Task<CompanyStructureApiResponse?> TryDeserializeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength == 0)
            return null;

        return await response.Content.ReadFromJsonAsync<CompanyStructureApiResponse>(
            JsonOptions,
            cancellationToken);
    }
}
