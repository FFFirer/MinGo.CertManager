using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Web.Extensions;

/// <summary>
/// 第三方 OAuth/OIDC 登录提供程序注册扩展方法
/// </summary>
public static class OAuthProviderExtensions
{
    /// <summary>
    /// 注册所有启用的第三方 OAuth/OIDC 登录提供程序。
    /// 通过程序集扫描自动发现所有 <see cref="IOAuthLoginProvider"/> 实现，
    /// 按 appsettings.json 中 <c>OAuthProviders.EnabledProviders</c> 数组顺序注册。
    /// </summary>
    public static IServiceCollection AddOAuthLoginProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var enabledProviders = configuration
            .GetSection("OAuthProviders:EnabledProviders")
            .Get<string[]>() ?? [];

        if(enabledProviders.Any() == false)
        {
            return services;
        }

        // 程序集扫描 → 字典（ProviderName → instance）
        var providerMap = DiscoverProviders();
        var authBuilder = services.AddAuthentication();

        // 按 EnabledProviders 数组顺序注册
        foreach (var providerName in enabledProviders)
        {
            if (!providerMap.TryGetValue(providerName, out var instance))
                continue;

            services.AddSingleton(typeof(IOAuthLoginProvider), instance);

            var displayName = GetDisplayName(configuration, providerName, instance.DisplayName);

            if (instance.AuthType == AuthenticationType.OpenIdConnect)
            {
                RegisterOpenIdConnectProvider(authBuilder, providerName, displayName, configuration);
            }
            else
            {
                RegisterOAuthProvider(authBuilder, instance, displayName, configuration);
            }
        }

        return services;
    }

    /// <summary>
    /// 从配置读取 DisplayName，不存在时使用默认值
    /// </summary>
    private static string GetDisplayName(IConfiguration configuration, string providerName, string defaultDisplayName)
    {
        return configuration[$"OAuthProviders:Providers:{providerName}:DisplayName"] ?? defaultDisplayName;
    }

    /// <summary>
    /// 为指定 provider 注册 OAuth 认证方案（AddOAuth）
    /// </summary>
    private static void RegisterOAuthProvider(
        AuthenticationBuilder builder,
        IOAuthLoginProvider provider,
        string displayName,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(
            $"OAuthProviders:Providers:{provider.ProviderName}");

        builder.AddOAuth(provider.ProviderName, displayName, options =>
        {
            // 关键：设置 SignInScheme 为 Identity.External
            // 这样 OAuth handler 完成流程后会写入外部登录凭据，
            // 后续 GetExternalLoginInfoAsync() 才能读取到
            options.SignInScheme = IdentityConstants.ExternalScheme;

            options.ClientId = section["ClientId"] ?? "";
            options.ClientSecret = section["ClientSecret"] ?? "";
            options.CallbackPath = new PathString(
                section["CallbackPath"] ?? $"/signin-{provider.ProviderName}");

            // 端点配置（支持在 appsettings.json 中覆盖）
            options.AuthorizationEndpoint = section["AuthorizationEndpoint"]
                ?? GetDefaultEndpoint(provider.ProviderName, "authorize");
            options.TokenEndpoint = section["TokenEndpoint"]
                ?? GetDefaultEndpoint(provider.ProviderName, "token");
            options.UserInformationEndpoint = section["UserInformationEndpoint"]
                ?? GetDefaultEndpoint(provider.ProviderName, "userinfo");

            // Scope 配置
            var scopes = section.GetSection("Scopes").Get<string[]>();
            if (scopes != null)
            {
                foreach (var scope in scopes)
                    options.Scope.Add(scope);
            }
            else
            {
                foreach (var scope in GetDefaultScopes(provider.ProviderName))
                    options.Scope.Add(scope);
            }

            options.SaveTokens = true;

            ConfigureOAuthEvents(options, provider.ProviderName);
        });
    }

    /// <summary>
    /// 为指定 provider 注册 OpenID Connect 认证方案（AddOpenIdConnect）
    /// </summary>
    private static void RegisterOpenIdConnectProvider(
        AuthenticationBuilder builder,
        string providerName,
        string displayName,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(
            $"OAuthProviders:Providers:{providerName}");

        builder.AddOpenIdConnect(providerName, displayName, options =>
        {
            options.SignInScheme = IdentityConstants.ExternalScheme;

            // Authority + 可选 Realm 拼接
            var authority = section["Authority"] ?? "";
            var realm = section["Realm"] ?? "";
            options.Authority = string.IsNullOrEmpty(realm)
                ? authority
                : $"{authority.TrimEnd('/')}/{realm}";

            options.ClientId = section["ClientId"] ?? "";
            options.ClientSecret = section["ClientSecret"] ?? "";
            options.CallbackPath = new PathString(
                section["CallbackPath"] ?? $"/signin-{providerName}");

            options.ResponseType = "code";

            // ⚠️ SimpleIdServer 可能不支持 email scope，需在 SimpleIdServer 端添加
            // 配置中可显式指定 Scopes 数组覆盖默认值
            var scopes = section.GetSection("Scopes").Get<string[]>();
            if (scopes != null)
            {
                foreach (var scope in scopes)
                    options.Scope.Add(scope);
            }
            else
            {
                options.Scope.Add("openid");
                options.Scope.Add("profile");
            }

            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = true;

            // Claim 映射
            options.MapInboundClaims = false;
            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.RoleClaimType = "role";

            options.ClaimActions.MapUniqueJsonKey("sub", "sub");
            options.ClaimActions.MapUniqueJsonKey("name", "name");
            options.ClaimActions.MapUniqueJsonKey("email", "email");
        });
    }

    /// <summary>
    /// 获取已知 provider 的默认 OAuth 端点
    /// </summary>
    private static string GetDefaultEndpoint(string providerName, string endpointType)
    {
        return (providerName, endpointType) switch
        {
            ("github", "authorize") => "https://github.com/login/oauth/authorize",
            ("github", "token") => "https://github.com/login/oauth/access_token",
            ("github", "userinfo") => "https://api.github.com/user",
            ("google", "authorize") => "https://accounts.google.com/o/oauth2/v2/auth",
            ("google", "token") => "https://oauth2.googleapis.com/token",
            ("google", "userinfo") => "https://www.googleapis.com/oauth2/v3/userinfo",
            _ => throw new NotSupportedException(
                $"Unknown provider '{providerName}'. " +
                "Provide AuthorizationEndpoint/TokenEndpoint/UserInformationEndpoint in configuration.")
        };
    }

    /// <summary>
    /// 获取已知 provider 的默认 scope
    /// </summary>
    private static string[] GetDefaultScopes(string providerName)
    {
        return providerName switch
        {
            "github" => ["read:user", "user:email"],
            "google" => ["profile", "email"],
            _ => ["openid", "profile", "email"]
        };
    }

    /// <summary>
    /// 配置 OAuth 事件。
    /// 注意：泛型 AddOAuth() 的 CreateTicketAsync 默认实现不调用 UserInformationEndpoint，
    /// 也不执行 ClaimActions（context.User 为空 {}）。
    /// 所有 claims 必须在此处通过手动调用用户信息 API 添加。
    /// </summary>
    private static void ConfigureOAuthEvents(OAuthOptions options, string providerName)
    {
        options.Events.OnCreatingTicket = async context =>
        {
            try
            {
                // 1. 手动调用用户信息端点
                var request = new HttpRequestMessage(
                    HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", context.AccessToken);
                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await context.Backchannel.SendAsync(
                    request, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                var userJson = await response.Content.ReadAsStringAsync();
                using var userDoc = JsonDocument.Parse(userJson);
                var user = userDoc.RootElement;

                // 2. 根据 provider 类型添加 claims
                switch (providerName)
                {
                    case "github":
                        AddGitHubClaims(context, user);
                        break;
                    case "google":
                        AddGoogleClaims(context, user);
                        break;
                    default:
                        AddDefaultClaims(context, user);
                        break;
                }
            }
            catch
            {
                // 用户信息获取失败不中断登录，后续进入邮箱补全页面
            }
        };
    }

    private static void AddGitHubClaims(OAuthCreatingTicketContext context, JsonElement user)
    {
        // id → NameIdentifier（GitHub 返回数字，需转字符串）
        if (user.TryGetProperty("id", out var id))
            context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.ToString()));

        // login → Name
        if (user.TryGetProperty("login", out var login) && login.ValueKind == JsonValueKind.String)
            context.Identity?.AddClaim(new Claim(ClaimTypes.Name, login.GetString()!));

        // email — 通常为 null，需要调用 /user/emails
        string? email = null;
        if (user.TryGetProperty("email", out var emailProp)
            && emailProp.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(emailProp.GetString()))
        {
            email = emailProp.GetString();
        }
        else
        {
            email = FetchGitHubPrimaryEmailAsync(context).GetAwaiter().GetResult();
        }

        if (!string.IsNullOrEmpty(email))
            context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email));
    }

    private static async Task<string?> FetchGitHubPrimaryEmailAsync(OAuthCreatingTicketContext context)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                context.Options.UserInformationEndpoint + "/emails");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", context.AccessToken);
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await context.Backchannel.SendAsync(
                request, context.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var emailsDoc = JsonDocument.Parse(content);

            foreach (var email in emailsDoc.RootElement.EnumerateArray())
            {
                if (email.GetProperty("primary").GetBoolean()
                    && email.GetProperty("verified").GetBoolean())
                {
                    return email.GetProperty("email").GetString();
                }
            }
        }
        catch
        {
            // 邮箱获取失败，后续进入邮箱补全页面
        }
        return null;
    }

    private static void AddGoogleClaims(OAuthCreatingTicketContext context, JsonElement user)
    {
        // sub → NameIdentifier
        if (user.TryGetProperty("sub", out var sub))
            context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, sub.GetString()!));

        // name → Name
        if (user.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            context.Identity?.AddClaim(new Claim(ClaimTypes.Name, name.GetString()!));

        // email
        if (user.TryGetProperty("email", out var email) && email.ValueKind == JsonValueKind.String)
            context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email.GetString()!));
    }

    private static void AddDefaultClaims(OAuthCreatingTicketContext context, JsonElement user)
    {
        if (user.TryGetProperty("sub", out var sub))
            context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, sub.GetString()!));
        if (user.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            context.Identity?.AddClaim(new Claim(ClaimTypes.Name, name.GetString()!));
        if (user.TryGetProperty("email", out var email) && email.ValueKind == JsonValueKind.String)
            context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email.GetString()!));
    }

    /// <summary>
    /// 扫描所有已加载程序集，查找 IOAuthLoginProvider 的非抽象实现类，
    /// 返回 ProviderName → instance 的字典
    /// </summary>
    private static Dictionary<string, IOAuthLoginProvider> DiscoverProviders()
    {
        var providers = new Dictionary<string, IOAuthLoginProvider>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes()
                    .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true }
                             && typeof(IOAuthLoginProvider).IsAssignableFrom(t)))
                {
                    if (Activator.CreateInstance(type) is IOAuthLoginProvider instance)
                    {
                        providers.TryAdd(instance.ProviderName, instance);
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 跳过无法加载类型的程序集
            }
        }

        return providers;
    }
}
