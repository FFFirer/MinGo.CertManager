## ADDED Requirements

### Requirement: OAuth Provider 接口定义

系统 SHALL 提供 `IOAuthLoginProvider` 接口作为第三方 OAuth 登录的标准扩展点。

- `ProviderName`: 提供程序唯一标识（如 "github"、"google"）
- `DisplayName`: UI 显示名称（如 "GitHub"、"Google"）
- `DisplayOrder`: 显示顺序（越小越靠前）
- `IconCssClass`: 按钮图标 CSS 类
- `Configure(AuthenticationBuilder, IConfiguration)`: 在启动时注册 OAuth 认证方案

#### Scenario: 新增 Provider 无需修改核心代码
- **WHEN** 开发者新增一个 `IOAuthLoginProvider` 实现类并配置 DI
- **THEN** 系统 SHALL 自动在登录页显示该 Provider 的登录按钮
- **THEN** 系统 SHALL 不需要修改 `Login.cshtml`、`ExternalLogin.cshtml` 或 `Program.cs`

### Requirement: GitHub OAuth 登录

系统 SHALL 支持使用 GitHub 账号登录。

- 使用 `AspNet.Security.OAuth.GitHub` NuGet 包
- 请求 scope: `user:email`
- 回调路径: `/signin-github`
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

- 使用 `Microsoft.AspNetCore.Authentication.Google`（SDK 内置）
- 请求 scope: `profile`、`email`
- 回调路径: `/signin-google`
- Claim 映射: `sub` → `NameIdentifier`，`name` → `Name`，`email` → `Email`

#### Scenario: Google 登录成功
- **WHEN** 用户点击"使用 Google 登录"按钮
- **THEN** 系统 SHALL 重定向到 Google OAuth 授权页
- **WHEN** 用户授权后 Google 回调
- **THEN** 系统 SHALL 通过 `ExternalLoginSignInAsync` 尝试自动登录
- **WHEN** 该 Google 账号已绑定本地用户
- **THEN** 系统 SHALL 登录成功并重定向到目标页面

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
- 状态为 `Active`，角色为 `User`
- 创建后调用 `UserManager.AddLoginAsync(user, externalLoginInfo)` 绑定
- 调用 `SignInManager.SignInAsync(user, false)` 登录

#### Scenario: OAuth 首次登录自动创建账号
- **WHEN** 用户通过 OAuth 首次登录且该邮箱无对应本地账号
- **THEN** 系统 SHALL 自动创建新本地账号（邮箱即用户名）
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

### Requirement: 登录页展示第三方登录按钮

系统 SHALL 在登录页展示所有已注册的第三方登录按钮。

- 按钮位于本地登录表单下方，以"或"分隔线隔开
- 按钮顺序按 `DisplayOrder` 排序
- 按钮显示 Provider 图标和名称
- 按钮点击后 POST 到 `ExternalLogin` Razor Page 的 `Challenge` handler

#### Scenario: 登录页显示第三方按钮
- **WHEN** 用户访问登录页
- **THEN** 系统 SHALL 在本地登录表单下方显示所有已启用 Provider 的登录按钮
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

### Requirement: 配置管理

OAuth Provider 的 ClientId 和 ClientSecret SHALL 通过配置管理。

- `appsettings.json` 定义 `OAuthProviders` 配置节
- `EnabledProviders` 数组控制启用哪些 Provider
- 每个 Provider 的 `ClientId` 和 `ClientSecret` 通过 User Secrets 注入
- `OAuthProviderExtensions.cs` 负责批量注册启用的 Provider

#### Scenario: 通过配置启用 Provider
- **WHEN** `appsettings.json` 的 `OAuthProviders.EnabledProviders` 包含 "github"
- **THEN** 系统 SHALL 在启动时注册 GitHub OAuth 认证方案
- **WHEN** 配置中不包含某个 Provider
- **THEN** 系统 SHALL 不注册该 Provider 的认证方案

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
