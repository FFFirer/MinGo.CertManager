## Purpose

第三方 OAuth 登录能力。定义标准的 Provider 扩展接口，支持 GitHub、Google 等第三方身份提供商登录，支持邮箱自动绑定已有账号和自动创建新账号。

## Requirements

### Requirement: OAuth Provider 接口定义

系统 SHALL 提供 `IOAuthLoginProvider` 接口作为第三方 OAuth/OIDC 登录的标准扩展点。

- `ProviderName`: 提供程序唯一标识（如 "github"、"simpleidserver"）
- `DisplayName`: UI 显示名称（如 "GitHub"、"SimpleIdServer"）
- `DisplayOrder`: 显示顺序（仍保留字段，排序优先级由 EnabledProviders 数组决定）
- `IconCssClass`: 按钮图标 CSS 类
- `AuthType`: 认证类型，`AuthenticationType.OAuth` 或 `AuthenticationType.OpenIdConnect`，默认为 `OAuth`

#### Scenario: 新增 OAuth/OIDC Provider 无需修改核心代码
- **WHEN** 开发者新增一个 `IOAuthLoginProvider` 实现类并配置 DI
- **THEN** 系统 SHALL 自动在登录页显示该 Provider 的登录按钮
- **THEN** 系统 SHALL 不需要修改 `Login.cshtml`、`ExternalLogin.cshtml` 或 `Program.cs`
- **WHEN** 该 Provider 的 `AuthType` 为 `OpenIdConnect`
- **THEN** 系统 SHALL 使用 `AddOpenIdConnect` 而非 `AddOAuth` 注册认证方案

### Requirement: GitHub OAuth 登录

系统 SHALL 支持使用 GitHub 账号登录。

- 使用通用 OAuth 2.0 流程（`AddOAuth`），无需 provider 特定 NuGet 包
- 回调路径: `/signin-github`
- 默认端点: `https://github.com/login/oauth/authorize`（授权）、`https://github.com/login/oauth/access_token`（令牌）、`https://api.github.com/user`（用户信息）
- 默认 scope: `read:user`、`user:email`
- Claim 映射: `id` → `NameIdentifier`，`login` → `Name`，`emails[0].value` → `Email`

#### Scenario: GitHub 登录成功
- **WHEN** 用户点击"使用 GitHub 登录"按钮
- **THEN** 系统 SHALL 重定向到 GitHub OAuth 授权页
- **WHEN** 用户授权后 GitHub 回调
- **THEN** 系统 SHALL 通过 `ExternalLoginSignInAsync` 尝试自动登录
- **WHEN** 该 GitHub 账号已绑定本地用户
- **THEN** 系统 SHALL 登录成功并重定向到目标页面

### Requirement: Google OAuth 登录

系统 SHALL 支持使用 Google 账号登录。

- 使用通用 OAuth 2.0 流程（`AddOAuth`），无需 provider 特定 NuGet 包
- 回调路径: `/signin-google`
- 默认端点: `https://accounts.google.com/o/oauth2/v2/auth`（授权）、`https://oauth2.googleapis.com/token`（令牌）、`https://www.googleapis.com/oauth2/v3/userinfo`（用户信息）
- 默认 scope: `profile`、`email`
- Claim 映射: `sub` → `NameIdentifier`，`name` → `Name`，`email` → `Email`

#### Scenario: Google 登录成功
- **WHEN** 用户点击"使用 Google 登录"按钮
- **THEN** 系统 SHALL 重定向到 Google OAuth 授权页
- **WHEN** 用户授权后 Google 回调
- **THEN** 系统 SHALL 通过 `ExternalLoginSignInAsync` 尝试自动登录
- **WHEN** 该 Google 账号已绑定本地用户
- **THEN** 系统 SHALL 登录成功并重定向到目标页面

### Requirement: SimpleIdServer OpenID Connect 登录

系统 SHALL 支持使用自建的 SimpleIdServer 作为 OpenID Connect 第三方登录提供程序。

- 使用 OpenID Connect 协议（`AddOpenIdConnect`），通过 `Authority` 自动发现端点
- `AuthenticationType` 为 `OpenIdConnect`
- 回调路径: `/signin-simpleidserver`
- 通过 `Authority` 配置，自动从 `/.well-known/openid-configuration` 发现端点
- **Realm 配置**: 支持通过可选 `Realm` 配置项指定 SimpleIdServer Realm，代码自动拼接为 `{Authority}/{Realm}`；未设置时 Authority 原样使用
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

### Requirement: 邮箱自动绑定

系统 SHALL 支持通过邮箱自动绑定外部 OAuth 账号到已有本地账号。

- OAuth 回调时获取 email
- 通过 `UserManager.FindByEmailAsync(email)` 查找本地用户
- 找到则调用 `UserManager.AddLoginAsync(user, externalLoginInfo)` 自动绑定
- 绑定完成后调用 `SignInManager.SignInAsync(user, false)` 登录

#### Scenario: 已有本地账号自动绑定
- **WHEN** 用户通过 OAuth 首次登录且该邮箱已存在本地账号
- **THEN** 系统 SHALL 自动将 OAuth Provider 绑定到该本地账号
- **THEN** 系统 SHALL 自动登录用户
- **THEN** 用户下次可用该 OAuth Provider 直接登录

### Requirement: 无匹配账号自动创建

系统 SHALL 支持在 OAuth 登录时自动创建新本地账号。

- OAuth 回调时获取 email
- `FindByEmailAsync` 未找到用户时，创建新 `ApplicationUser`
- 新用户：`UserName = email`，`Email = email`，`EmailConfirmed = true`
- 状态为 `Active`
- 角色通过 `ApplicationUserManager.CreateAsync` 默认机制自动赋予 "User"，无需显式调用
- 创建后调用 `UserManager.AddLoginAsync(user, externalLoginInfo)` 绑定
- 调用 `SignInManager.SignInAsync(user, false)` 登录

#### Scenario: OAuth 首次登录自动创建账号
- **WHEN** 用户通过 OAuth 首次登录且该邮箱无对应本地账号
- **THEN** 系统 SHALL 自动创建新本地账号（邮箱即用户名）
- **THEN** 系统 SHALL 通过默认角色机制自动赋予 "User" 角色
- **THEN** 系统 SHALL 自动绑定 OAuth Provider
- **THEN** 系统 SHALL 自动登录用户

### Requirement: 邮箱为空处理

系统 SHALL 处理 OAuth Provider 未返回邮箱的情况。

- GitHub 允许用户隐藏邮箱，API 可能不返回 email
- 在 OAuth Callback 中检测 email 是否为空
- 为空时跳转到邮箱补全页面，要求用户手动输入

#### Scenario: GitHub 邮箱隐藏
- **WHEN** GitHub 用户隐藏了邮箱且 OAuth 回调时 email 为空
- **THEN** 系统 SHALL 跳转到邮箱补全页面
- **WHEN** 用户输入邮箱并提交
- **THEN** 系统 SHALL 按正常绑定/创建流程继续

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

### Requirement: 登录页展示第三方登录按钮

系统 SHALL 在登录页展示所有已注册的第三方登录按钮，按 `EnabledProviders` 数组顺序排列。

- 按钮位于本地登录表单下方，以"或"分隔线隔开
- 按钮顺序按 `EnabledProviders` 数组顺序
- 按钮显示 Provider 图标和自定义显示名称
- 按钮点击后 POST 到 `ExternalLogin` Razor Page 的 `Challenge` handler

#### Scenario: 登录页显示第三方按钮（按配置顺序）
- **WHEN** 用户访问登录页
- **THEN** 系统 SHALL 在本地登录表单下方显示所有已启用 Provider 的登录按钮
- **THEN** 按钮 SHALL 按 `EnabledProviders` 数组顺序排列
- **THEN** 每个按钮 SHALL 显示对应的 Provider 图标和名称

### Requirement: 邮箱登录兼容

系统 SHALL 支持使用邮箱作为登录标识，并兼容现有 username 登录。

- `Login.cshtml.cs` 的 InputModel 增加 Email 字段
- 验证时先用 `FindByEmailAsync(email)` 查找
- 查不到再用 `FindByNameAsync(email)` 兼容旧数据
- `IdentitySeedService` 创建 admin 时 `UserName = "admin@example.com"` 与 Email 一致

#### Scenario: 邮箱登录
- **WHEN** 用户在登录页输入邮箱和密码
- **THEN** 系统 SHALL 按邮箱查找用户并验证密码
- **WHEN** 密码正确
- **THEN** 系统 SHALL 登录成功

### Requirement: 角色常量定义

系统 SHALL 提供 `RoleConstants` 静态类，集中定义所有角色名常量。

- `RoleConstants.Admin` = `"Admin"`
- `RoleConstants.User` = `"User"`
- 角色常量的引用位置：`MinGo.CertManager.Core.Constants`

#### Scenario: 角色常量可被所有层引用
- **WHEN** 任何项目层（Core/Infrastructure/Web/Application）需要引用角色名
- **THEN** 系统 SHALL 通过 `RoleConstants.Admin` 或 `RoleConstants.User` 获取，而非硬编码字符串

### Requirement: 默认角色机制

系统 SHALL 确保任何通过 `UserManager.CreateAsync` 创建的新 `ApplicationUser` 自动获得 "User" 角色。

- 通过自定义 `ApplicationUserManager`（继承 `UserManager<ApplicationUser>`）重写 `CreateAsync` 方法实现
- 无需在调用方显式调用 `AddToRoleAsync(user, "User")`
- Admin 用户创建时自动获得 "User" 角色，同时保留后续赋予的 "Admin" 角色

#### Scenario: 外部登录新用户自动获得 User 角色
- **WHEN** 用户通过 OAuth/OIDC 首次登录且系统自动创建新账号
- **THEN** `ApplicationUserManager.CreateAsync` 创建用户后自动调用 `AddToRoleAsync(user, "User")`
- **THEN** 新用户拥有 "User" 角色
- **THEN** 调用方无需再次调用 `AddToRoleAsync`

#### Scenario: Admin 用户默认也拥有 User 角色
- **WHEN** 启动时种子服务创建 Admin 用户（通过 `UserManager.CreateAsync`）
- **THEN** Admin 用户自动获得 "User" 角色
- **THEN** 种子服务再为其显式添加 "Admin" 角色
- **THEN** Admin 用户同时拥有 "User" 和 "Admin" 两个角色

### Requirement: 配置管理

OAuth/OIDC Provider 的 ClientId 和 ClientSecret SHALL 通过配置管理。OpenID Connect Provider 额外支持 Authority 配置。

- `appsettings.json` 定义 `OAuthProviders` 配置节
- `EnabledProviders` 数组控制启用哪些 Provider 及其显示顺序
- 每个 Provider 的 `DisplayName` 可在配置中覆盖
- OAuth Provider 的端点、scope、Claim 映射可在配置中覆盖
- OIDC Provider 通过 `Authority` 地址自动发现端点
- OIDC Provider 支持可选的 `Realm` 字段，有值时自动拼接为 `{Authority}/{Realm}`
- 每个 Provider 的 `ClientId` 和 `ClientSecret` 通过 User Secrets 注入
- `OAuthProviderExtensions.cs` 负责批量注册启用的 Provider，按 `AuthType` 分流 OAuth/OIDC

#### Scenario: 通过配置启用 OAuth Provider
- **WHEN** `appsettings.json` 的 `OAuthProviders.EnabledProviders` 包含 "github"
- **THEN** 系统 SHALL 在启动时注册 GitHub OAuth 认证方案
- **WHEN** 配置中不包含某个 Provider
- **THEN** 系统 SHALL 不注册该 Provider 的认证方案

#### Scenario: 通过配置启用 OIDC Provider（带 Realm）
- **WHEN** `appsettings.json` 的 `OAuthProviders.EnabledProviders` 包含 "simpleidserver"
- **THEN** 系统 SHALL 在启动时通过 `AddOpenIdConnect` 注册 SimpleIdServer 认证方案
- **THEN** 系统 SHALL 根据配置中的 `Authority` 和 `Realm` 拼接 OIDC Authority URL
- **WHEN** `Realm` 设为 `"master"`
- **THEN** 最终 Authority 为 `{Authority}/master`，discovery 请求到 `{Authority}/master/.well-known/openid-configuration`

### Requirement: 安全要求

系统 SHALL 遵循以下安全规范：

- ClientSecret 通过 User Secrets 管理，不提交到 git
- 使用 ASP.NET Core 标准的 `AntiForgeryToken` 防止 CSRF
- OAuth 流程使用 `ChallengeResult` 标准实现
- 回调路径遵循 OAuth2 协议，使用 HTTPS

#### Scenario: Secret 安全存储
- **WHEN** 开发环境配置 OAuth Provider
- **THEN** ClientSecret SHALL 通过 `dotnet user-secrets set` 命令注入
- **THEN** `appsettings.json` 中的 ClientSecret 字段 SHALL 为空字符串

### Requirement: 已登录用户绑定外部账号

系统 SHALL 支持已登录用户在 Profile 页主动绑定新的第三方外部账号，而非仅在登录流程中自动绑定。

- ExternalLogin PageModel SHALL 新增 `OnPostLinkLogin` handler
- `OnPostLinkLogin` SHALL 通过 `SignInManager.ConfigureExternalAuthenticationProperties` 发起 OAuth Challenge
- 回调地址 SHALL 指向 `LinkLoginCallback` 而非 `Callback`
- ExternalLogin PageModel SHALL 新增 `OnGetLinkLoginCallbackAsync` handler
- `LinkLoginCallback` SHALL 检测当前用户是否已登录，未登录则重定向到登录页
- `LinkLoginCallback` SHALL 通过 `GetExternalLoginInfoAsync()` 获取外部登录信息
- `LinkLoginCallback` SHALL 调用 `UserManager.AddLoginAsync(user, info)` 完成绑定
- 绑定成功后 SHALL 调用 `SignInManager.RefreshSignInAsync(user)` 刷新 cookie
- 绑定完成后 SHALL 重定向到 Profile 页

#### Scenario: 已登录用户从 Profile 页发起绑定
- **WHEN** 用户已在登录状态且点击 Profile 页的绑定按钮
- **THEN** 系统 SHALL POST 到 ExternalLogin 的 LinkLogin handler
- **THEN** 系统 SHALL 发起 OAuth Challenge 重定向到第三方 Provider

#### Scenario: 绑定回调成功
- **WHEN** 用户在第三方 Provider 完成授权后回调到 LinkLoginCallback
- **THEN** 系统 SHALL 通过 `GetExternalLoginInfoAsync()` 获取登录信息
- **THEN** 系统 SHALL 调用 `AddLoginAsync` 绑定到当前用户
- **THEN** 系统 SHALL 刷新登录 cookie 并重定向到 Profile 页

#### Scenario: 绑定回调时用户未登录
- **WHEN** 用户未登录状态访问 LinkLoginCallback
- **THEN** 系统 SHALL 重定向到登录页
