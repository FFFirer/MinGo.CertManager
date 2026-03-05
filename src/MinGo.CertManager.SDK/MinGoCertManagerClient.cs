using System.Net.Http; using MinGo.CertManager.SDK.Services;

namespace MinGo.CertManager.SDK;

public class MinGoCertManagerClient
{
    public CertificateService CertificateService { get; }

    public MinGoCertManagerClient(string apiKey, string baseUrl)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new System.Uri(baseUrl)
        };

        var apiClient = new ApiClient(httpClient, apiKey);
        CertificateService = new CertificateService(apiClient);
    }

    public MinGoCertManagerClient(string apiKey, string baseUrl, HttpClient httpClient)
    {
        httpClient.BaseAddress = new System.Uri(baseUrl);
        var apiClient = new ApiClient(httpClient, apiKey);
        CertificateService = new CertificateService(apiClient);
    }
}