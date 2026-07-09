## Context

当前系统使用 `IOAuthLoginProvider` 接口 + `OAuthProviderExtensions` 程序集扫描的方式注册第三方 OAuth 2.0 登录（GitHub、Google）。所有 Provider 均通过 `AddOAuth()` 注册，属于通用 OAuth 2.0 流程。

现需要添加自建的 SimpleIdServer（OpenID Connect 身份提供商）作为第三方登录选项。SimpleIdServer 使用标准的 OIDC 协议，支持自动端点发现（`.well-known/openid-configuration`），这与现有的 `AddOAuth` 手动配置端点的方式不同。

本项目使用 ASP.NET Core Identity (`IdentityConstants.ExternalScheme`) + Blazor Server，ExternalLogin 回调流程完全通用。

## Goals / Non-Goals

**Goals:**
- `IOAuthLoginProvider` 接口扩展支持区分 OAuth 和 OpenID Connect 两种认证类型
- `OAuthProviderExtensions` 支持按类型分流注册：OAuth → `AddOAuth()`，OIDC → `AddOpenIdConnect()`
- 新增 `SimpleIdServerOAuthLoginProvider` 实现，使用 OIDC 协议（`Authority` 方式）
- 支持通过配置文件 `OAuthProviders.Providers.simpleidserver.DisplayName` 自定义按钮显示名称
- 登录按钮排序改为按 `EnabledProviders` 数组顺序
- 保持现有 GitHub/Google Provider 完全不受影响

**Non-Goals:**
- 不支持运行时动态注册 Provider（仅在启动时扫描）
- 不引入 SimpleIdServer.OpenIdConnect NuGet 包（使用 ASP.NET Core 内置 OIDC 中间件）
- 不改动 ExternalLogin 回调流程（现有的自动绑定/创建账号逻辑完全适用）

## Decisions

### 1. 接口扩展策略：添加默认属性（而非新接口）

**选择**: 在 `IOAuthLoginProvider` 上添加 `AuthenticationType` 默认属性

```csharp
public enum AuthenticationType { OAuth, OpenIdConnect }

public interface IOAuthLoginProvider
{
    string ProviderName { get; }
    string DisplayName { get; }
    int DisplayOrder { get; }
    string IconCssClass { get; }
    AuthenticationType AuthType => AuthenticationType.OAuth; // 默认 OAuth
}
```

**理由**: 
- 默认值 OAuth 确保现有 GitHub/Google Provider 无需任何修改
- 统一扫描逻辑不变（`DiscoverProviderTypes` 仍只扫描一个接口）
- 无需重复注册/管理机制

### 2. DisplayName 自定义：配置覆盖两层策略

**选择**: `OAuthProviderExtensions` 注册时检查 `section["DisplayName"]`，有则覆盖 class 默认值

```csharp
var displayName = section["DisplayName"] ?? provider.DisplayName;
// 传入 AddOAuth/AddOpenIdConnect 的 displayName 参数
```

**配置示例**:
```json
{
  "simpleidserver": {
    "DisplayName": "公司统一账号",
    "ClientId": "",
    "ClientSecret": "",
    "Authority": "https://your-server/master"
  }
}
```

**理由**: 与现有端点覆盖模式一致（`section["AuthorizationEndpoint"] ?? GetDefaultEndpoint(...)`），零学习成本。

### 3. 排序策略：按 EnabledProviders 数组顺序

**选择**: 按 `EnabledProviders` 数组顺序注册 Provider，`Login.cshtml` 去掉 `.OrderBy()` 直接使用注入顺序

**理由**:
- DI 注册顺序 = `IEnumerable<T>` 解析顺序
- `OAuthProviderExtensions` 已按 `EnabledProviders` 顺序遍历，注册顺序自然匹配数组顺序
- 用户明确要求此行为

### 4. OIDC 注册使用标准 AddOpenIdConnect

**选择**: 使用 `builder.AddOpenIdConnect()`，不引入 SimpleIdServer.OpenIdConnect 包

```csharp
builder.AddOpenIdConnect(provider.ProviderName, displayName, options =>
{
    options.SignInScheme = IdentityConstants.ExternalScheme;
    options.Authority = section["Authority"] ?? "";
    options.ClientId = section["ClientId"] ?? "";
    options.ClientSecret = section["ClientSecret"] ?? "";
    options.CallbackPath = new PathString(section["CallbackPath"] ?? $"/signin-{provider.ProviderName}");
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.SaveTokens = true;
    // Claim 映射通过 ClaimActions 自动处理
});
```

**理由**: 
- `AddOpenIdConnect` 是 ASP.NET Core 共享框架的一部分，无需额外依赖
- SimpleIdServer 完全兼容标准 OIDC 协议
- OIDC 中间件自动通过 discovery document 发现端点
- OIDC 内置 `ClaimActions` 自动映射 claims（`sub`→`sub`, `name`→`name`, `email`→`email`），无需手动 `OnCreatingTicket`

### 5. Claim 映射使用 OIDC 内置机制

**选择**: 使用 `ClaimActions` + `GetClaimsFromUserInfoEndpoint`，不手写 `OnCreatingTicket`

**理由**: 
- OIDC 中间件自动从 id_token 提取 claims
- `GetClaimsFromUserInfoEndpoint = true` 时自动调用 userinfo 端点
- `ClaimActions.MapUniqueJsonKey("sub", "sub")` 自动映射标准 OIDC claims
- 与现有 `ExternalLogin.cshtml.cs` 的 `ClaimTypes.Email` / `ClaimTypes.NameIdentifier` 读取兼容

### 6. CallbackPath 保持与现有模式一致

**选择**: OIDC Provider 使用 `/signin-{providerName}`（即 `/signin-simpleidserver`），而非默认的 `/signin-oidc`

**理由**: 
- 与现有 GitHub (`/signin-github`)、Google (`/signin-google`) 保持统一
- 多 Provider 时必须唯一，自定义路径是最清晰的方式
- SimpleIdServer 端 Redirect URI 配置为 `https://your-app/signin-simpleidserver`

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|---------|
| `AddOpenIdConnect` 和 `AddOAuth` 对 `SignInScheme` 的默认处理可能不同 | 显式设置 `SignInScheme = IdentityConstants.ExternalScheme`，确保一致性 |
| OIDC discovery 依赖 SimpleIdServer 的 `.well-known/openid-configuration` 端点可达 | Authority URL 通过配置管理，可随时调整；开发环境可用 `RequireHttpsMetadata = false` |
| SimpleIdServer 可能返回非标准 claim 名 | 通过 `options.ClaimActions.MapUniqueJsonKey()` 显式映射，或使用 `OnCreatingTicket` 兜底 |
| 多个 OIDC Provider 的 CallbackPath 冲突 | 每个 Provider 使用 `/signin-{providerName}` 命名模式确保唯一 |
