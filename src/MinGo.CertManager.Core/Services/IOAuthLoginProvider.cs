namespace MinGo.CertManager.Core.Services;

/// <summary>
/// 第三方认证类型的枚举
/// </summary>
public enum AuthenticationType
{
    /// <summary>通用 OAuth 2.0（使用 AddOAuth 注册）</summary>
    OAuth,

    /// <summary>OpenID Connect（使用 AddOpenIdConnect 注册）</summary>
    OpenIdConnect
}

/// <summary>
/// 第三方 OAuth/OIDC 登录提供程序接口。
/// 实现此接口即可添加一个新的第三方登录方式，无需修改核心登录流程。
/// </summary>
public interface IOAuthLoginProvider
{
    /// <summary>
    /// 提供程序唯一标识，如 "github"、"simpleidserver"
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// UI 显示名称，如 "GitHub"、"SimpleIdServer"
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// 显示顺序，越小越靠前（由 EnabledProviders 数组顺序决定，此字段保留用于参考）
    /// </summary>
    int DisplayOrder { get; }

    /// <summary>
    /// 按钮图标 CSS 类，如 "fab fa-github"
    /// </summary>
    string IconCssClass { get; }

    /// <summary>
    /// 认证类型，默认为 OAuth
    /// </summary>
    AuthenticationType AuthType => AuthenticationType.OAuth;
}
