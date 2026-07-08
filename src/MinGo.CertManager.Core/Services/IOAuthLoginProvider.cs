namespace MinGo.CertManager.Core.Services;

/// <summary>
/// 第三方 OAuth 登录提供程序接口。
/// 实现此接口即可添加一个新的第三方登录方式，无需修改核心登录流程。
/// </summary>
public interface IOAuthLoginProvider
{
    /// <summary>
    /// 提供程序唯一标识，如 "github"、"google"
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// UI 显示名称，如 "GitHub"、"Google"
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// 显示顺序，越小越靠前
    /// </summary>
    int DisplayOrder { get; }

    /// <summary>
    /// 按钮图标 CSS 类，如 "fab fa-github"
    /// </summary>
    string IconCssClass { get; }
}
