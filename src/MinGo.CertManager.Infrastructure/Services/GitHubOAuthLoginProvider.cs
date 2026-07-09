using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// GitHub OAuth 登录提供程序（元数据定义）。
/// OAuth 认证方案的具体注册由 Web 层的 OAuthProviderExtensions 完成。
/// </summary>
public class GitHubOAuthLoginProvider : IOAuthLoginProvider
{
    public string ProviderName => "github";
    public string DisplayName => "GitHub";
    public int DisplayOrder => 10;
    public string IconCssClass => "fab fa-github";
}
