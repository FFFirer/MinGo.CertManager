## Why

当前系统仅支持用户名+密码的本地登录方式，缺乏第三方 OAuth 登录能力。引入第三方登录可以：
- 降低用户使用门槛（无需额外注册即可用 GitHub/Google 账号登录）
- 为未来接入企业微信、飞书等企业级身份提供商奠定基础
- 遵循标准的 ASP.NET Core Identity ExternalLogin 机制，与现有认证体系无缝集成

## What Changes

- 定义 `IOAuthLoginProvider` 接口，作为第三方登录提供方的标准扩展点
- 实现 GitHub OAuth 登录提供程序
- 实现 Google OAuth 登录提供程序
- 新增 `ExternalLogin` Razor Page，处理 OAuth Challenge 和 Callback
- 修改登录页 (`Login.cshtml`)，在本地登录表单下方追加第三方登录按钮
- 支持邮箱绑定策略：优先绑定已有本地账号（按邮箱匹配），未匹配时自动创建新账号
- 登录方式改为邮箱作为唯一标识（支持邮箱查找并兼容现有 username 登录）
- 支持"一个本地账号绑定多个外部 Provider"
- 使用 User Secrets 管理 OAuth ClientId/ClientSecret，不提交到代码库

## Capabilities

### New Capabilities
- `oauth-login`: 第三方 OAuth 登录核心能力，包含 Provider 接口定义、Challenge/Callback 流程、本地账号绑定/创建策略

### Modified Capabilities
无。当前没有现有 specs。

## Impact

- **新增依赖**：`AspNet.Security.OAuth.GitHub` NuGet 包（Google 使用 SDK 内置包无需额外安装）
- **新增文件**：
  - `Core/Services/IOAuthLoginProvider.cs` — Provider 接口
  - `Infrastructure/Services/GitHubOAuthLoginProvider.cs` — GitHub 实现
  - `Infrastructure/Services/GoogleOAuthLoginProvider.cs` — Google 实现
  - `Web/Pages/ExternalLogin.cshtml` + `.cshtml.cs` — 外部登录 Razor Page
  - `Web/Extensions/OAuthProviderExtensions.cs` — DI 注册扩展方法
- **修改文件**：
  - `Web/Pages/Login.cshtml` — 追加第三方登录按钮
  - `Web/Pages/Login.cshtml.cs` — 支持邮箱登录
  - `Web/Program.cs` — 添加一行 `AddOAuthLoginProviders()` 调用
  - `Web/appsettings.json` — 新增 `OAuthProviders` 配置节
  - `Infrastructure/Services/IdentitySeedService.cs` — admin 账号邮箱一致性
- **数据库**：无需迁移，使用 Identity 内置 `AspNetUserLogins` 表
- **安全性**：ClientSecret 通过 dotnet user-secrets 注入；使用 ASP.NET Core 标准的 AntiForgeryToken 和 ChallengeResult 流程
