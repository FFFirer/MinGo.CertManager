using MinGo.CertManager.Core.Entities;

namespace MinGo.CertManager.Core.Services;

/// <summary>
/// 域名 FQDN 推导与 Zone 匹配工具
/// </summary>
public static class DomainNameHelper
{
    /// <summary>
    /// 由 RR + Zone 推导 FQDN。
    /// "@" → zone；"www" → www.zone；"*" → *.zone
    /// </summary>
    public static string ToFqdn(string zoneName, string rr)
    {
        if (string.IsNullOrWhiteSpace(zoneName))
        {
            throw new ArgumentException("Zone name is required", nameof(zoneName));
        }

        var zone = zoneName.Trim().TrimEnd('.').ToLowerInvariant();
        var relative = (rr ?? string.Empty).Trim().TrimEnd('.');

        if (relative.Length == 0 || relative == "@")
        {
            return zone;
        }

        return $"{relative.ToLowerInvariant()}.{zone}";
    }

    /// <summary>
    /// 从 FQDN 找到最长匹配的已购 Zone。无匹配返回 null。
    /// </summary>
    public static string? MatchZone(string fqdn, IEnumerable<string> zoneNames)
    {
        if (string.IsNullOrWhiteSpace(fqdn))
        {
            return null;
        }

        var name = fqdn.Trim().TrimEnd('.').ToLowerInvariant();
        string? best = null;

        foreach (var zone in zoneNames)
        {
            var z = zone.Trim().TrimEnd('.').ToLowerInvariant();
            if (z.Length == 0)
            {
                continue;
            }

            if (name == z || name.EndsWith("." + z, StringComparison.Ordinal))
            {
                if (best is null || z.Length > best.Length)
                {
                    best = z;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// 判断 FQDN 是否为泛域名
    /// </summary>
    public static bool IsWildcard(string fqdn)
    {
        return !string.IsNullOrWhiteSpace(fqdn) && fqdn.Trim().StartsWith("*.", StringComparison.Ordinal);
    }
}
