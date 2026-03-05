using System.Text.Json.Serialization;

namespace MinGo.CertManager.SDK.Models;

public class ErrorResponse
{
    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    [JsonPropertyName("Error")]
    public string Error { get; set; }
}