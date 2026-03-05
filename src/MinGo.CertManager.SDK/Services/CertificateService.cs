using System; using System.Net.Http; using System.Net.Http.Json; using System.Threading.Tasks; using MinGo.CertManager.Core.Entities; using MinGo.CertManager.SDK.Models;

namespace MinGo.CertManager.SDK.Services;

public class CertificateService
{
    private readonly ApiClient _apiClient;

    public CertificateService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<CertificateResponse> RequestCertificateAsync(CertificateRequest request)
    {
        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("api/external/certificates", UriKind.Relative),
            Content = JsonContent.Create(request)
        };

        return await _apiClient.SendAsync<CertificateResponse>(httpRequest);
    }

    public async Task<byte[]> DownloadCertificateAsync(string domain, CertificateFormat format, string? password = null)
    {
        var queryParams = $"?format={format}";
        if (!string.IsNullOrEmpty(password))
        {
            queryParams += $"&password={Uri.EscapeDataString(password)}";
        }

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"api/external/certificates/{Uri.EscapeDataString(domain)}/download{queryParams}", UriKind.Relative)
        };

        return await _apiClient.DownloadAsync(httpRequest);
    }
}