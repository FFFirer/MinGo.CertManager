## ADDED Requirements

### Requirement: SimpleIdServer OpenID Connect 登录

系统 SHALL 支持使用自建的 SimpleIdServer 作为 OpenID Connect 第三方登录提供程序。

- 使用 OpenID Connect 协议（`AddOpenIdConnect`），通过 `Authority` 自动发现端点
- `AuthenticationType` 为 `OpenIdConnect`
- 回调路径: `/signin-simpleidserver`
- 通过 `Authority` 配置，自动从 `/.well-known/openid-configuration` 发现端点
- 默认 scope: `openid`、`profile`、`email`
- Claim 映射: 通过 OIDC 中间件内置 `ClaimActions` + `GetClaimsFromUserInfoEndpoint`
- 图标: `fas fa-id-card`
- SimpleIdServer 服务端需配置 Redirect URI 为应用地址的 `/signin-simpleidserver`

#### Scenario: SimpleIdServer 登录成功
- **WHEN** 用户点击"使用 SimpleIdServer 登录"按钮
- **THEN** 系统 SHALL 发起 OIDC Challenge 重定向到 SimpleIdServer 授权页
- **WHEN** 用户授权后 SimpleIdServer 回调到 `/signin-simpleidserver`
- **THEN** 系统 SHALL 通过 `ExternalLoginSignInAsync` 尝试自动登录
- **WHEN** 该 SimpleIdServer 账号已绑定本地用户
- **THEN** 系统 SHALL 登录成功并重定向到目标页面

#### Scenario: SimpleIdServer 首次登录自动创建账号
- **WHEN** 用户通过 SimpleIdServer 首次登录且该邮箱无对应本地账号
- **THEN** 系统 SHALL 自动创建新本地账号（邮箱即用户名）
- **THEN** 系统 SHALL 自动绑定 OIDC Provider
- **THEN** 系统 SHALL 自动登录用户

### Requirement: Provider DisplayName 可自定义

系统 SHALL 支持在 `appsettings.json` 中为每个 Provider 自定义显示名称。

- 配置节: `OAuthProviders.Providers.{providerName}.DisplayName`
- 当配置存在时覆盖 `IOAuthLoginProvider.DisplayName` 默认值
- 配置不存在时使用 Provider 类中的默认值

#### Scenario: 通过配置自定义显示名称
- **WHEN** `appsettings.json` 中 `OAuthProviders.Providers.simpleidserver.DisplayName` 设为 `"公司统一账号"`
- **THEN** 登录页按钮显示"使用 公司统一账号 登录"
- **WHEN** 该配置项不存在
- **THEN** 按钮显示"使用 SimpleIdServer 登录"

### Requirement: Provider 排序按 EnabledProviders 数组顺序

系统 SHALL 按 `OAuthProviders.EnabledProviders` 数组中的顺序显示第三方登录按钮。

- 不再使用 `IOAuthLoginProvider.DisplayOrder` 进行排序
- 数组第一个元素显示在最上方

#### Scenario: 按数组顺序显示
- **WHEN** `EnabledProviders` 为 `["simpleidserver", "github", "google"]`
- **THEN** 登录页按钮顺序为 SimpleIdServer → GitHub → Google

## MODIFIED Requirements

### Requirement: OAuth Provider 接口定义（扩展认证类型）

系统 SHALL 提供 `IOAuthLoginProvider` 接口作为第三方 OAuth/OIDC 登录的标准扩展点。

- `ProviderName`: 提供程序唯一标识（如 "github"、"simpleidserver"）
- `DisplayName`: UI 显示名称（如 "GitHub"、"SimpleIdServer"）
- `DisplayOrder`: 显示顺序（仍保留字段，排序优先级由 EnabledProviders 数组决定）
- `IconCssClass`: 按钮图标 CSS 类
- `AuthType`: 认证类型，`AuthenticationType.OAuth` 或 `AuthenticationType.OpenIdConnect`，默认为 `OAuth`

#### Scenario: 新增 OIDC Provider 无需修改核心代码
- **WHEN** 开发者新增一个 `IOAuthLoginProvider` 实现类（`AuthType = OpenIdConnect`）并配置 DI
- **THEN** 系统 SHALL 自动在登录页显示该 Provider 的登录按钮
- **THEN** 系统 SHALL 使用 `AddOpenIdConnect` 而非 `AddOAuth` 注册认证方案
- **THEN** 系统 SHALL 不需要修改 `Login.cshtml`、`ExternalLogin.cshtml` 或 `Program.cs`

### Requirement: 配置管理（扩展 OIDC 配置）

OAuth Provider 的 ClientId 和 ClientSecret SHALL 通过配置管理。OpenID Connect Provider 额外支持 Authority 配置。

- `appsettings.json` 定义 `OAuthProviders` 配置节
- `EnabledProviders` 数组控制启用哪些 Provider 及其显示顺序
- 每个 Provider 的 `DisplayName` 可在配置中覆盖
- OAuth Provider 的端点、scope、Claim 映射可在配置中覆盖
- OIDC Provider 通过 `Authority` 地址自动发现端点
- 每个 Provider 的 `ClientId` 和 `ClientSecret` 通过 User Secrets 注入
- `OAuthProviderExtensions.cs` 负责批量注册启用的 Provider，按 `AuthType` 分流 OAuth/OIDC

#### Scenario: 通过配置启用 OIDC Provider
- **WHEN** `appsettings.json` 的 `OAuthProviders.EnabledProviders` 包含 "simpleidserver"
- **THEN** 系统 SHALL 在启动时通过 `AddOpenIdConnect` 注册 SimpleIdServer 认证方案
- **THEN** 系统 SHALL 根据配置中的 `Authority` 自动发现 OIDC 端点
- **WHEN** 配置中不包含某个 Provider
- **THEN** 系统 SHALL 不注册该 Provider 的认证方案

### Requirement: 登录页展示第三方登录按钮（按数组顺序）

系统 SHALL 在登录页展示所有已注册的第三方登录按钮，按 `EnabledProviders` 数组顺序排列。

- 按钮位于本地登录表单下方，以"或"分隔线隔开
- 按钮顺序按 `EnabledProviders` 数组顺序
- 按钮显示 Provider 图标和名称
- 按钮点击后 POST 到 `ExternalLogin` Razor Page 的 `Challenge` handler

#### Scenario: 登录页显示第三方按钮（按配置顺序）
- **WHEN** 用户访问登录页
- **THEN** 系统 SHALL 在本地登录表单下方显示所有已启用 Provider 的登录按钮
- **THEN** 按钮 SHALL 按 `EnabledProviders` 数组顺序排列
- **THEN** 每个按钮 SHALL 显示对应的 Provider 图标和名称
