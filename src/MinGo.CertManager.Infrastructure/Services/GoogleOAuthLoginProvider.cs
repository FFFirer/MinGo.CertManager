using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// Google OAuth 登录提供程序（元数据定义）。
/// OAuth 认证方案的具体注册由 Web 层的 OAuthProviderExtensions 完成。
/// </summary>
public class GoogleOAuthLoginProvider : IOAuthLoginProvider
{
    public string ProviderName => "google";
    public string DisplayName => "Google";
    public int DisplayOrder => 20;
    public string IconCssClass => "fab fa-google";
}
