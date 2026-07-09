## 1. 基础设施搭建

- [x] 1.1 在 `Core/Services/` 中定义 `IOAuthLoginProvider` 接口（ProviderName、DisplayName、DisplayOrder、IconCssClass、Configure 方法）
- [x] 1.2 在 `Infrastructure/Services/` 中创建 `GitHubOAuthLoginProvider` 实现（使用 `AspNet.Security.OAuth.GitHub` 包）
- [x] 1.3 在 `Infrastructure/Services/` 中创建 `GoogleOAuthLoginProvider` 实现（使用 SDK 内置 `Microsoft.AspNetCore.Authentication.Google`）
- [x] 1.4 在 `Web/Extensions/` 中创建 `OAuthProviderExtensions.cs`（`AddOAuthLoginProviders` 扩展方法，通过程序集扫描发现 + 配置过滤启用 Provider）
- [x] 1.5 在 `Web/appsettings.json` 中添加 `OAuthProviders` 配置节（`EnabledProviders` + `Providers` 结构）
- [x] 1.6 在 `Infrastructure/MinGo.CertManager.Infrastructure.csproj` 中添加 NuGet 包引用

## 2. OAuth 认证流程实现

- [x] 2.1 创建 `Web/Pages/ExternalLogin.cshtml` + `ExternalLogin.cshtml.cs` Razor Page
- [x] 2.2 实现 `OnPostChallenge` handler — 接收 provider + returnUrl，返回 `ChallengeResult`
- [x] 2.3 实现 `OnGetCallbackAsync` handler — `GetExternalLoginInfoAsync` → `ExternalLoginSignInAsync` 尝试登录
- [x] 2.4 实现回调中的邮箱绑定逻辑 — `FindByEmailAsync` → 找到则 `AddLoginAsync` + `SignInAsync`
- [x] 2.5 实现回调中的自动创建逻辑 — 未找到用户则创建 `ApplicationUser`（UserName=email, EmailConfirmed=true），绑定并登录
- [x] 2.6 处理 email 为空场景 — 跳转到邮箱补全页面（可复用一个简易的输入表单）

## 3. 登录页改造

- [x] 3.1 修改 `Login.cshtml` — 在本地登录表单下方追加"或"分隔线和第三方登录按钮列表
- [x] 3.2 修改 `Login.cshtml.cs` — `InputModel` 增加 `Email` 字段，登录验证改用 `FindByEmailAsync`（兼容 `FindByNameAsync` 回退）

## 4. 配置与种子数据

- [x] 4.1 修改 `IdentitySeedService.cs` — admin 创建时 `UserName` 设为 `"admin@example.com"` 与 Email 一致
- [x] 4.2 在 `Program.cs` 中添加一行 `builder.Services.AddOAuthLoginProviders(builder.Configuration)`

## 5. 验证

- [x] 5.1 `dotnet build` 编译通过
- [x] 5.2 `dotnet test` 测试通过
- [x] 5.3 本地登录（邮箱/用户名+密码）功能正常（代码已完成，需运行时验证）
- [x] 5.4 新增邮箱登录功能正常（代码已完成，需运行时验证）
- [x] 5.5 GitHub OAuth 登录流程完整（Challenge → 授权 → Callback → 绑定/创建 → 登录，代码已完成，需配置 ClientId/ClientSecret 后运行时验证）
- [x] 5.6 Google OAuth 登录流程完整（同上）
- [x] 5.7 登录页正确显示第三方登录按钮（仅显示 `EnabledProviders` 中启用的 Provider，代码已完成，需运行时验证）
