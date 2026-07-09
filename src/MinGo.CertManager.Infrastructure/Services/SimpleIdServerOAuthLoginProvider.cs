using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// SimpleIdServer OpenID Connect 登录提供程序（元数据定义）。
/// OIDC 认证方案的具体注册由 Web 层的 OAuthProviderExtensions 完成。
/// </summary>
public class SimpleIdServerOAuthLoginProvider : IOAuthLoginProvider
{
    public string ProviderName => "simpleidserver";
    public string DisplayName => "SimpleIdServer";
    public int DisplayOrder => 0;
    public string IconCssClass => "fas fa-id-card";
    public AuthenticationType AuthType => AuthenticationType.OpenIdConnect;
}
