using System.Text.Json.Serialization;

namespace MinGo.CertManager.SDK.Models;

public class ErrorResponse
{
    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    [JsonPropertyName("Error")]
    public required string Error { get; set; }
}