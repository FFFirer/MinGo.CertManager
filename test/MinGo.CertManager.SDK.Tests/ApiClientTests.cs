using System; using System.Net; using System.Net.Http; using System.Text; using System.Threading; using System.Threading.Tasks; using MinGo.CertManager.SDK; using MinGo.CertManager.SDK.Exceptions; using MinGo.CertManager.SDK.Models; using Moq; using Moq.Protected; using Xunit;

namespace MinGo.CertManager.SDK.Tests;

public class ApiClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly ApiClient _apiClient;

    public ApiClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        _apiClient = new ApiClient(_httpClient, "test-api-key");
    }

    [Fact]
    public async Task SendAsync_ShouldAddAuthHeader()
    {
        // Arrange
        var responseContent = new StringContent("{\"Success\": true}", Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                Assert.Equal("ApiKey", request.Headers.Authorization?.Scheme);
                Assert.Equal("test-api-key", request.Headers.Authorization?.Parameter);
            })
            .ReturnsAsync(response);

        // Act
        var result = await _apiClient.SendAsync<CertificateResponse>(new HttpRequestMessage(HttpMethod.Get, "/test"));

        // Assert
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ShouldThrowAuthenticationException_WhenUnauthorized()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(() =>
            _apiClient.SendAsync<CertificateResponse>(new HttpRequestMessage(HttpMethod.Get, "/test")));
    }

    [Fact]
    public async Task SendAsync_ShouldThrowApiException_WhenBadRequest()
    {
        // Arrange
        var responseContent = new StringContent("{\"Success\": false, \"Error\": \"Bad request\"}", Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = responseContent };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _apiClient.SendAsync<CertificateResponse>(new HttpRequestMessage(HttpMethod.Get, "/test")));
        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("Bad request", exception.Message);
    }

    [Fact]
    public async Task DownloadAsync_ShouldAddAuthHeader()
    {
        // Arrange
        var responseContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                Assert.Equal("ApiKey", request.Headers.Authorization?.Scheme);
                Assert.Equal("test-api-key", request.Headers.Authorization?.Parameter);
            })
            .ReturnsAsync(response);

        // Act
        var result = await _apiClient.DownloadAsync(new HttpRequestMessage(HttpMethod.Get, "/test"));

        // Assert
        Assert.NotNull(result);
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}