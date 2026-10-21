using System.Net;
using System.Net.Sockets;

namespace MinGo.CertManager.Core.Services;

/// <summary>
/// 解析记录创建校验。仅允许 A / AAAA / CNAME，默认 TTL 600。
/// </summary>
public static class DnsRecordValidator
{
    public const int DefaultTtl = 600;
    public const int MinTtl = 600;
    public const int MaxTtl = 86400;

    public const string TypeA = "A";
    public const string TypeAaaa = "AAAA";
    public const string TypeCname = "CNAME";

    /// <summary>ACME DNS-01 专用 RR 前缀，禁止手动删除</summary>
    public const string AcmeChallengeRrPrefix = "_acme-challenge.";

    public static readonly IReadOnlySet<string> AllowedTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { TypeA, TypeAaaa, TypeCname };

    /// <summary>
    /// 是否允许手动管理的记录类型。
    /// </summary>
    public static bool IsAllowedType(string? recordType) =>
        !string.IsNullOrWhiteSpace(recordType) && AllowedTypes.Contains(recordType.Trim());

    /// <summary>
    /// RR 是否属于 ACME 验证记录（含根形式 _acme-challenge）。
    /// </summary>
    public static bool IsAcmeChallengeRr(string? rr)
    {
        if (string.IsNullOrWhiteSpace(rr))
        {
            return false;
        }

        var value = rr.Trim().TrimEnd('.').ToLowerInvariant();
        return value == "_acme-challenge" || value.StartsWith(AcmeChallengeRrPrefix, StringComparison.Ordinal);
    }

    /// <summary>
    /// 创建前校验。成功返回 null，失败返回错误消息。
    /// </summary>
    public static string? ValidateCreate(string? rr, string? recordType, string? value, int ttl)
    {
        if (string.IsNullOrWhiteSpace(rr))
        {
            return "RR（主机记录）不能为空";
        }

        var type = (recordType ?? string.Empty).Trim().ToUpperInvariant();
        if (!IsAllowedType(type))
        {
            return "仅支持 A、AAAA、CNAME 记录类型";
        }

        if (IsAcmeChallengeRr(rr))
        {
            return "不允许直接管理 _acme-challenge 记录（证书验证占用）";
        }

        if (type.Equals(TypeCname, StringComparison.Ordinal) &&
            (rr.Trim() == "@" || rr.Trim().Length == 0))
        {
            return "根域名（@）不支持 CNAME，请改用 A/AAAA 或子域名";
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return "记录值不能为空";
        }

        var valueError = type switch
        {
            TypeA => IsValidIpv4(value) ? null : "A 记录值必须是合法 IPv4 地址",
            TypeAaaa => IsValidIpv6(value) ? null : "AAAA 记录值必须是合法 IPv6 地址",
            _ => IsValidCnameTarget(value) ? null : "CNAME 记录值必须是合法主机名"
        };

        if (valueError is not null)
        {
            return valueError;
        }

        if (ttl < MinTtl || ttl > MaxTtl)
        {
            return $"TTL 需在 {MinTtl}-{MaxTtl} 秒之间（默认 {DefaultTtl}）";
        }

        return null;
    }

    /// <summary>
    /// 删除前校验。成功返回 null，失败返回错误消息。
    /// </summary>
    public static string? ValidateDelete(string? rr)
    {
        if (IsAcmeChallengeRr(rr))
        {
            return "不允许删除 _acme-challenge 记录（证书验证占用）";
        }

        return null;
    }

    private static bool IsValidIpv4(string value) =>
        IPAddress.TryParse(value.Trim(), out var ip) && ip.AddressFamily == AddressFamily.InterNetwork;

    private static bool IsValidIpv6(string value) =>
        IPAddress.TryParse(value.Trim(), out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6;

    private static bool IsValidCnameTarget(string value)
    {
        // CNAME 目标必须是主机名，不能是 IP 字面量
        if (IsValidIpv4(value) || IsValidIpv6(value))
        {
            return false;
        }

        return IsValidHostname(value);
    }

    private static bool IsValidHostname(string value)
    {
        var host = value.Trim().TrimEnd('.');
        if (host.Length == 0 || host.Length > 253)
        {
            return false;
        }

        if (host.StartsWith("*.", StringComparison.Ordinal))
        {
            // CNAME 目标不接受泛域名
            return false;
        }

        foreach (var label in host.Split('.'))
        {
            if (label.Length == 0 || label.Length > 63)
            {
                return false;
            }

            if (label.StartsWith('-') || label.EndsWith('-'))
            {
                return false;
            }

            foreach (var ch in label)
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_'))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
