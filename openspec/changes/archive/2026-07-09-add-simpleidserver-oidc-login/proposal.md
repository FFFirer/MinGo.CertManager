## Why

当前系统支持 GitHub 和 Google 的第三方 OAuth 登录，但缺乏对自建 OpenID Connect 身份提供商的支持。需要添加 SimpleIdServer（自建 OIDC 服务器）作为第三方登录选项，以便企业内部用户通过统一身份认证登录系统。

## What Changes

- 扩展 `IOAuthLoginProvider` 接口，增加 `AuthenticationType` 属性以区分 OAuth 和 OpenID Connect 两种认证类型
- 新增 `SimpleIdServerOAuthLoginProvider` 实现，使用 OpenID Connect 协议
- 扩展 `OAuthProviderExtensions`，新增 OpenID Connect Provider 注册逻辑（`AddOpenIdConnect`）
- 支持通过配置文件自定义 Provider 的显示名称（`DisplayName`）
- 登录按钮排序规则改为按 `EnabledProviders` 数组顺序，而非硬编码的 `DisplayOrder`
- SimpleIdServer Provider 的图标使用 `fas fa-id-card`

## Capabilities

### New Capabilities

- `simpleidserver-login`: SimpleIdServer OpenID Connect 第三方登录实现，包括 Provider 元数据定义、OIDC 认证注册、登录页按钮显示

### Modified Capabilities

- `oauth-login`: `IOAuthLoginProvider` 接口扩展支持两种认证类型（OAuth/OIDC）；`OAuthProviderExtensions` 扩展支持按类型分流注册；配置文件支持 DisplayName 自定义

## Impact

- **接口变更**: `IOAuthLoginProvider` 新增 `AuthenticationType` 属性（默认值 OAuth，向后兼容）
- **新增文件**: `MinGo.CertManager.Infrastructure/Services/SimpleIdServerOAuthLoginProvider.cs`
- **修改文件**: 
  - `MinGo.CertManager.Core/Services/IOAuthLoginProvider.cs`
  - `MinGo.CertManager.Web/Extensions/OAuthProviderExtensions.cs`
  - `MinGo.CertManager.Web/Pages/Login.cshtml`
  - `MinGo.CertManager.Web/appsettings.json`
  - `openspec/specs/oauth-login/spec.md`
- **无影响**: `GitHubOAuthLoginProvider`、`GoogleOAuthLoginProvider`、`Program.cs`、`ExternalLogin` 系列文件、`Login.cshtml.cs`
- **新增依赖**: 无（`AddOpenIdConnect` 属于 ASP.NET Core 共享框架，无需额外 NuGet 包）
