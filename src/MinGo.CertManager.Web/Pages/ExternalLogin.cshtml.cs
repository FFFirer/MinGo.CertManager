using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Web.Pages;

/// <summary>
/// 第三方 OAuth 登录页面
/// 处理 Challenge 发起、Callback 回调、邮箱绑定/自动创建流程
/// </summary>
[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<ExternalLoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>返回 URL</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>是否在邮箱补全流程中</summary>
    public bool IsNoEmailFlow { get; set; }

    /// <summary>Provider 显示名称</summary>
    public string? ProviderDisplayName { get; set; }

    /// <summary>Provider 名称</summary>
    public string? ProviderName { get; set; }

    // ================================================================
    // 1. Challenge 发起 — 用户点击第三方登录按钮时调用
    // ================================================================

    /// <summary>
    /// 发起 OAuth Challenge，重定向到第三方 Provider 的授权页
    /// </summary>
    public IActionResult OnPostChallenge(string provider, string returnUrl = "/")
    {
        if (string.IsNullOrEmpty(provider))
        {
            return RedirectToPage("/Login");
        }

        var redirectUrl = Url.Page("./ExternalLogin", "Callback",
            new { returnUrl });
        var properties = _signInManager
            .ConfigureExternalAuthenticationProperties(provider, redirectUrl);

        return new ChallengeResult(provider, properties);
    }

    // ================================================================
    // 2. OAuth Callback — Provider 授权后重定向回来
    // ================================================================

    /// <summary>
    /// OAuth 回调处理：自动登录或绑定/创建本地账号
    /// </summary>
    public async Task<IActionResult> OnGetCallbackAsync(
        string returnUrl = "/", string? remoteError = null)
    {
        var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        _logger.LogInformation("External auth: Succeeded={Succeeded}, HasPrincipal={HasPrincipal}, HasProps={HasProps}, ItemsCount={ItemsCount}",
            authResult.Succeeded,
            authResult.Principal != null,
            authResult.Properties != null,
            authResult.Properties?.Items?.Count ?? 0);

        if (remoteError != null)
        {
            _logger.LogError("External auth error from provider: {Error}", remoteError);
            return RedirectToPage("/Login");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("ExternalLoginInfo is null - possible expired temp cookie");
            return RedirectToPage("/Login");
        }

        // 1) 尝试自动登录（已有绑定记录）
        var signInResult = await _signInManager
            .ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
                isPersistent: false, bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            _logger.LogInformation(
                "User logged in via {Provider} with provider key {ProviderKey}",
                info.LoginProvider, info.ProviderKey);
            return LocalRedirect(returnUrl);
        }

        if (signInResult.IsLockedOut)
        {
            return RedirectToPage("/Login");
        }

        // 2) 获取邮箱
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(email))
        {
            // 跳转到邮箱补全页面
            return RedirectToPage("./ExternalLogin", "NoEmail", new
            {
                returnUrl,
                provider = info.LoginProvider,
                providerDisplayName = info.ProviderDisplayName ?? info.LoginProvider
            });
        }

        // 3) 按邮箱查找已有本地账号 → 自动绑定
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
            if (addLoginResult.Succeeded)
            {
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                _logger.LogInformation(
                    "Bound {Provider} account to existing user {Email}",
                    info.LoginProvider, email);
                return LocalRedirect(returnUrl);
            }

            // 绑定失败（可能是该 provider 已被其他账号绑定）
            foreach (var error in addLoginResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return RedirectToPage("/Login");
        }

        // 4) 未找到已有用户 → 自动创建新账号
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true, // OAuth Provider 已验证邮箱
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to create user from {Provider}: {Errors}",
                info.LoginProvider,
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return RedirectToPage("/Login");
        }

        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, isPersistent: false);

        _logger.LogInformation(
            "Created new user {Email} via {Provider} login",
            email, info.LoginProvider);

        return LocalRedirect(returnUrl);
    }

    // ================================================================
    // 3. 邮箱补全 — OAuth 未返回邮箱时用户手动输入
    // ================================================================

    /// <summary>
    /// 显示邮箱补全页面
    /// </summary>
    public IActionResult OnGetNoEmailAsync(
        string returnUrl = "/",
        string? provider = null,
        string? providerDisplayName = null)
    {
        IsNoEmailFlow = true;
        ReturnUrl = returnUrl;
        ProviderName = provider;
        ProviderDisplayName = providerDisplayName;
        return Page();
    }

    /// <summary>
    /// 处理邮箱补全提交
    /// </summary>
    public async Task<IActionResult> OnPostCompleteEmailAsync(
        string returnUrl = "/",
        string? provider = null,
        string? email = null)
    {
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError(string.Empty, "请输入邮箱");
            IsNoEmailFlow = true;
            ReturnUrl = returnUrl;
            ProviderName = provider;
            return Page();
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError(string.Empty, "邮箱格式不正确");
            IsNoEmailFlow = true;
            ReturnUrl = returnUrl;
            ProviderName = provider;
            return Page();
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToPage("/Login");
        }

        // 重新执行绑定/创建流程（与 Callback 中的逻辑一致）
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
            if (addLoginResult.Succeeded)
            {
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                _logger.LogInformation(
                    "Bound {Provider} to existing user {Email} (email prompt)",
                    info.LoginProvider, email);
                return LocalRedirect(returnUrl);
            }
            return RedirectToPage("/Login");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to create user from email prompt: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return RedirectToPage("/Login");
        }

        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, isPersistent: false);

        return LocalRedirect(returnUrl);
    }

    // ================================================================
    // 4. 绑定外部账号 — 已登录用户从 Profile 页发起
    // ================================================================

    /// <summary>
    /// 发起 OAuth Challenge 用于绑定外部账号到当前用户
    /// (POST 版本 — 由 Login 页 Razor form 使用)
    /// </summary>
    public IActionResult OnPostLinkLogin(string provider, string returnUrl = "/profile")
    {
        if (string.IsNullOrEmpty(provider))
        {
            return RedirectToPage("/Profile");
        }

        var redirectUrl = Url.Page("./ExternalLogin", "LinkLoginCallback",
            new { returnUrl });
        var properties = _signInManager
            .ConfigureExternalAuthenticationProperties(provider, redirectUrl);

        return new ChallengeResult(provider, properties);
    }

    /// <summary>
    /// 发起 OAuth Challenge 用于绑定外部账号到当前用户
    /// (GET 版本 — 由 Profile Blazor 页使用)
    /// </summary>
    public IActionResult OnGetLinkLogin(string provider, string returnUrl = "/profile")
    {
        return OnPostLinkLogin(provider, returnUrl);
    }

    /// <summary>
    /// 绑定回调 — 将 Provider 账号绑定到当前已登录用户
    /// </summary>
    public async Task<IActionResult> OnGetLinkLoginCallbackAsync(string returnUrl = "/profile")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning(
                "LinkLogin callback failed: ExternalLoginInfo is null (possible expired temp cookie)");
            return RedirectToPage(returnUrl);
        }

        var result = await _userManager.AddLoginAsync(user, info);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation(
                "User {UserId} linked {Provider} account {ProviderKey}",
                user.Id, info.LoginProvider, info.ProviderKey);
        }
        else
        {
            foreach (var error in result.Errors)
            {
                _logger.LogWarning(
                    "Failed to link {Provider} to user {UserId}: {Error}",
                    info.LoginProvider, user.Id, error.Description);
            }
        }

        return RedirectToPage(returnUrl);
    }
}
