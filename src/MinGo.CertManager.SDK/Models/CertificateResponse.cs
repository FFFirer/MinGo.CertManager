using System; using System.Text.Json.Serialization;

namespace MinGo.CertManager.SDK.Models;

public class CertificateResponse
{
    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    [JsonPropertyName("CertificateId")]
    public Guid CertificateId { get; set; }

    [JsonPropertyName("Domain")]
    public string Domain { get; set; }

    [JsonPropertyName("Status")]
    public string Status { get; set; }

    [JsonPropertyName("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("ExpiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("Error")]
    public string Error { get; set; }
}