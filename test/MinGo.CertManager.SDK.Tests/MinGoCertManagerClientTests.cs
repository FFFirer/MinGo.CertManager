using System.Net.Http; using MinGo.CertManager.SDK; using MinGo.CertManager.SDK.Services; using Xunit;

namespace MinGo.CertManager.SDK.Tests;

public class MinGoCertManagerClientTests
{
    [Fact]
    public void Constructor_ShouldInitializeCertificateService()
    {
        // Arrange & Act
        var client = new MinGoCertManagerClient("test-api-key", "https://api.example.com");

        // Assert
        Assert.NotNull(client.CertificateService);
        Assert.IsType<CertificateService>(client.CertificateService);
    }

    [Fact]
    public void Constructor_WithHttpClient_ShouldInitializeCertificateService()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var client = new MinGoCertManagerClient("test-api-key", "https://api.example.com", httpClient);

        // Assert
        Assert.NotNull(client.CertificateService);
        Assert.IsType<CertificateService>(client.CertificateService);
    }
}