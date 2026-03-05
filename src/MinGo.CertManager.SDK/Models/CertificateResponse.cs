using System; using System.Text.Json.Serialization;

namespace MinGo.CertManager.SDK.Models;

public class CertificateResponse
{
    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    [JsonPropertyName("CertificateId")]
    public Guid CertificateId { get; set; }

    [JsonPropertyName("Domain")]
    public required string Domain { get; set; }

    [JsonPropertyName("Status")]
    public required string Status { get; set; }

    [JsonPropertyName("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("ExpiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("Error")]
    public required string Error { get; set; }
}