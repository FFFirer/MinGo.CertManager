using System.Text.Json.Serialization;

namespace MinGo.CertManager.SDK.Models;

public class CertificateRequest
{
    [JsonPropertyName("Domain")]
    public string Domain { get; set; }

    [JsonPropertyName("IsWildcard")]
    public bool IsWildcard { get; set; }

    [JsonPropertyName("UseStaging")]
    public bool UseStaging { get; set; }

    public CertificateRequest(string domain, bool isWildcard = false, bool useStaging = false)
    {
        Domain = domain;
        IsWildcard = isWildcard;
        UseStaging = useStaging;
    }
}