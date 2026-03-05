using System; using System.Net.Http; using System.Net.Http.Headers; using System.Text; using System.Text.Json; using System.Threading.Tasks; using MinGo.CertManager.SDK.Exceptions; using MinGo.CertManager.SDK.Models;

namespace MinGo.CertManager.SDK;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ApiClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    private void LogApiCall(string method, string url)
    {
        // 记录API调用信息，包括apikey的前8位（用于识别但不暴露完整密钥）
        var apiKeyPrefix = _apiKey.Length > 8 ? _apiKey.Substring(0, 8) + "..." : _apiKey;
        Console.WriteLine($"[{DateTime.UtcNow}] API Call: {method} {url} - ApiKey: {apiKeyPrefix}");
    }

    public virtual async Task<T> SendAsync<T>(HttpRequestMessage request)
    {
        AddAuthHeader(request);

        // 记录API调用
        var url = request.RequestUri?.ToString() ?? "unknown";
        LogApiCall(request.Method.ToString(), url);

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException("Authentication failed: Invalid API key");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorResponse errorResponse;

                try
                {
                    errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                }
                catch
                {
                    throw new ApiException($"API request failed with status code: {response.StatusCode}", (int)response.StatusCode);
                }

                throw new ApiException(errorResponse.Error, (int)response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }
        catch (HttpRequestException ex)
        {
            throw new SdkException("Network error occurred", ex);
        }
        catch (JsonException ex)
        {
            throw new SdkException("Failed to parse API response", ex);
        }
    }

    public virtual async Task<byte[]> DownloadAsync(HttpRequestMessage request)
    {
        AddAuthHeader(request);

        // 记录API调用
        var url = request.RequestUri?.ToString() ?? "unknown";
        LogApiCall(request.Method.ToString(), url);

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException("Authentication failed: Invalid API key");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorResponse errorResponse;

                try
                {
                    errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                }
                catch
                {
                    throw new ApiException($"API request failed with status code: {response.StatusCode}", (int)response.StatusCode);
                }

                throw new ApiException(errorResponse.Error, (int)response.StatusCode);
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (HttpRequestException ex)
        {
            throw new SdkException("Network error occurred", ex);
        }
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", _apiKey);
    }
}