namespace MinGo.CertManager.Core.Services;

/// <summary>
/// 凭据脱敏展示工具。Secret 永不进入展示模型。
/// </summary>
public static class CredentialMaskHelper
{
    /// <summary>AccessKeyId 可见前缀长度</summary>
    public const int AccessKeyIdVisiblePrefixLength = 6;

    /// <summary>统一掩码后缀</summary>
    public const string MaskSuffix = "****";

    /// <summary>
    /// AccessKeyId 脱敏：保留前缀，其余以掩码替代；空值返回空串。
    /// </summary>
    public static string MaskAccessKeyId(string? accessKeyId)
    {
        if (string.IsNullOrWhiteSpace(accessKeyId))
        {
            return string.Empty;
        }

        var value = accessKeyId.Trim();
        if (value.Length <= AccessKeyIdVisiblePrefixLength)
        {
            return MaskSuffix;
        }

        return value[..AccessKeyIdVisiblePrefixLength] + MaskSuffix;
    }

    /// <summary>
    /// 是否已提供非空凭据字段。
    /// </summary>
    public static bool IsProvided(string? value) => !string.IsNullOrWhiteSpace(value);
}
