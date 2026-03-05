using System; using System.Net; using System.Net.Http; using System.Text; using System.Threading; using System.Threading.Tasks; using MinGo.CertManager.Core.Entities; using MinGo.CertManager.SDK; using MinGo.CertManager.SDK.Models; using MinGo.CertManager.SDK.Services; using Moq; using Xunit;

namespace MinGo.CertManager.SDK.Tests;

public class CertificateServiceTests
{
    private readonly Mock<ApiClient> _mockApiClient;
    private readonly CertificateService _certificateService;

    public CertificateServiceTests()
    {
        _mockApiClient = new Mock<ApiClient>(MockBehavior.Strict, new HttpClient(), "test-api-key");
        _certificateService = new CertificateService(_mockApiClient.Object);
    }

    [Fact]
    public async Task RequestCertificateAsync_ShouldCallApiClient()
    {
        // Arrange
        var request = new CertificateRequest("example.com");
        var expectedResponse = new CertificateResponse
        {
            Success = true,
            CertificateId = Guid.NewGuid(),
            Domain = "example.com",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            Error = string.Empty
        };

        _mockApiClient
            .Setup(x => x.SendAsync<CertificateResponse>(It.IsAny<HttpRequestMessage>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _certificateService.RequestCertificateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.CertificateId, result.CertificateId);
        Assert.Equal(expectedResponse.Domain, result.Domain);
        _mockApiClient.Verify(x => x.SendAsync<CertificateResponse>(It.IsAny<HttpRequestMessage>()), Times.Once);
    }

    [Fact]
    public async Task DownloadCertificateAsync_ShouldCallApiClient()
    {
        // Arrange
        var domain = "example.com";
        var format = CertificateFormat.Pfx;
        var password = "test-password";
        var expectedResponse = new byte[] { 1, 2, 3 };

        _mockApiClient
            .Setup(x => x.DownloadAsync(It.IsAny<HttpRequestMessage>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _certificateService.DownloadCertificateAsync(domain, format, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse, result);
        _mockApiClient.Verify(x => x.DownloadAsync(It.IsAny<HttpRequestMessage>()), Times.Once);
    }

    [Fact]
    public async Task DownloadCertificateAsync_ShouldCallApiClient_WithoutPassword()
    {
        // Arrange
        var domain = "example.com";
        var format = CertificateFormat.Pem;
        string? password = null;
        var expectedResponse = new byte[] { 1, 2, 3 };

        _mockApiClient
            .Setup(x => x.DownloadAsync(It.IsAny<HttpRequestMessage>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _certificateService.DownloadCertificateAsync(domain, format, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse, result);
        _mockApiClient.Verify(x => x.DownloadAsync(It.IsAny<HttpRequestMessage>()), Times.Once);
    }
}