## Context

当前系统基于 ASP.NET Core Identity 的 cookie 认证，仅支持用户名+密码本地登录。应用架构为 Razor Pages + Blazor Server 混合：登录/登出使用 Razor Pages（全页刷新），业务页面使用 Blazor Server（SignalR 电路）。

关键约束：
- 项目遵循 Clean Architecture（Core / Infrastructure / Application / Web）
- Identity 已配置 `AddIdentity<ApplicationUser, IdentityRole>()`，使用 `SignInManager.PasswordSignInAsync`
- 登录页 `Pages/Login.cshtml` 为 Razor Page，表单 POST 提交
- `AspNetUserLogins` 表（Identity 内置）已存在，无需迁移
- 现有 DI 模式：`AddScoped` + 接口驱动，配置用 `IOptions<T>` + `SectionName` 惯用名

## Goals / Non-Goals

**Goals:**
- 定义标准接口 `IOAuthLoginProvider`，新增第三方登录只需实现该接口 + 配置
- 实现 GitHub 和 Google 两个 OAuth Provider
- 与现有 Identity ExternalLogin 机制无缝集成（`ChallengeResult`、`ExternalLoginSignInAsync`、`AddLoginAsync`）
- 支持邮箱绑定：按邮箱匹配已有账号自动绑定，无匹配时自动创建
- 登录方式改为邮箱作为唯一标识，兼容现有 username 登录
- 所有 ClientSecret 通过 User Secrets 注入，不提交代码库

**Non-Goals:**
- 不存储额外字段（如 AvatarUrl、DisplayName），`ApplicationUser` 不做扩展
- 不实现前端 SPA 风格登录（保持 Razor Page 表单 POST 模式）
- 不涉及 API Key 认证体系改造
- 不做动态 Provider 运行时热加载（Provider 在启动时注册）

## Decisions

### 1. Provider 扩展方式：接口驱动 vs 直接配置

**选择：接口驱动。**

| 方案 | 评价 |
|------|------|
| 直接 `Program.cs` 中 `AddGitHub()` / `AddGoogle()` | ❌ 每次加 Provider 改 Program.cs，分散 |
| 接口 + DI 自动发现 | ✅ 加 Provider = 新建一个类，不改现有文件 |
| 配置文件动态注册 | ❌ 过度抽象，Provider 逻辑复杂多变 |

`IOAuthLoginProvider` 接口将 Provider 特定的认证方案注册封装在 `Configure()` 方法内，内部调用 `builder.AddGitHub()` 或 `builder.AddOAuth()`。DI 自动发现所有实现并批量注册。

### 2. OAuth 回调处理：Razor Page vs Controller

**选择：Razor Page (`Pages/ExternalLogin.cshtml`)。**

- 与现有 `Login.cshtml` / `Logout.cshtml` 同层，共享 `_Layout.cshtml`
- 与 ASP.NET Core Identity 内置 UI 的 `ExternalLogin` Razor Page 模式一致
- `ChallengeResult` 作为 `ActionResult` 在 Razor Page 的 `OnPost` 中返回
- 登录成功后 `LocalRedirect` 回到 Blazor 应用 URL

### 3. 本地账号绑定策略：邮箱自动绑定 + 自动创建

**流程：**
1. OAuth Callback 获取 email
2. `FindByEmailAsync(email)` 查找已有用户
   - 找到 → `AddLoginAsync(user, info)` 自动绑定 → 登录
   - 未找到 → 创建新用户（`UserName = email`）→ `AddLoginAsync` 绑定 → 登录
3. 如果 OAuth 未返回 email → 跳转到补全邮箱页面

**理由：** OAuth Provider 已验证邮箱所有权，等价于"用 GitHub 证明你就是邮箱的主人"，不需要额外密码验证。

### 4. 邮箱作为登录标识

- `IdentitySeedService.cs` 创建 admin 时 `UserName = "admin@example.com"`（与 Email 一致）
- `Login.cshtml.cs` 的 `InputModel`：增加 `Email` 字段，移除 `Username`
- 登录验证：`FindByEmailAsync(email)` → 查不到再用 `FindByNameAsync(email)` 兼容旧数据
- 外部登录自动创建的用户：`UserName = email`

### 5. 两次 `AddAuthentication()` 时序问题

`AddIdentity<ApplicationUser, IdentityRole>()` 内部已调用 `AddAuthentication()` + `AddCookie()`。后续 `services.AddAuthentication()` 复用已有的 `AuthenticationBuilder`，追加新 scheme 不影响已有配置。这是 ASP.NET Core 官方推荐的追加模式。

### 6. NuGet 依赖策略

| Provider | 包 | 来源 | 理由 |
|----------|------|------|------|
| Google | `Microsoft.AspNetCore.Authentication.Google` | .NET SDK 内置 | 微软官方维护，无需额外安装 |
| GitHub | `AspNet.Security.OAuth.GitHub` | aspnet-contrib 社区 | 最广泛使用的 GitHub OAuth 包，处理了 GitHub 特有的 claim 映射和 scope |

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         请求路由                                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Login.cshtml (Razor Page)                                          │
│  ┌─────────────────────────────────────────────┐                    │
│  │ 本地登录表单 (POST)                          │                    │
│  │ ─────── 或 ───────                          │                    │
│  │ [使用 GitHub 登录] → POST ExternalLogin?handler=Challenge       │
│  │ [使用 Google 登录]  → POST ExternalLogin?handler=Challenge       │
│  └─────────────────┬───────────────────────────┘                    │
│                    │ ChallengeResult("github")                      │
│                    ▼                                                 │
│       重定向到 GitHub OAuth 授权页                                    │
│                    │                                                 │
│                    ▼                                                 │
│       用户授权 → 回调 /signin-github                                 │
│                    │                                                 │
│                    ▼                                                 │
│  ExternalLogin.cshtml (Razor Page)                                   │
│  ┌─────────────────────────────────────┐                            │
│  │ OnGetCallbackAsync                   │                           │
│  │ 1. GetExternalLoginInfoAsync()       │                           │
│  │ 2. ExternalLoginSignInAsync() → ✅   │                           │
│  │ 3. 否: FindByEmailAsync(email)       │                           │
│  │    ├─ 找到: AddLoginAsync + SignIn   │                           │
│  │    └─ 未找到: CreateAsync + Login    │                           │
│  │ 4. LocalRedirect(returnUrl)          │                           │
│  └─────────────────────┬───────────────┘                            │
│                        │                                            │
│                        ▼                                            │
│  Blazor SPA (/certificates)                                         │
│  Circuit 重建 → Cookie 生效 → 已登录状态                              │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|---------|
| GitHub 用户可能隐藏邮箱导致 email 为空 | OAuth 回调中检测 email 为空时跳转到手动输入邮箱页面 |
| 两个不同的 OAuth 账号绑定到同一个本地邮箱 | Identity 允许多个 LoginProvider 关联同一 UserId，这是支持的场景 |
| admin 账号不能通过 OAuth 绑定 | ExternalLogin Callback 中检查 `FindByEmailAsync` 返回 admin 时走正常绑定流程（admin 账户仍可被绑定，但登录时按邮箱查到的 admin 账号绑定 OAuth 不会赋予外部用户管理员权限） |
| Blazor Circuit 重建后认证状态同步 | Cookie 认证自动恢复；`AuthenticationStateProvider` 在 Blazor 电路建立时读取 cookie |
| `GetExternalLoginInfoAsync` 依赖 temp cookie，可能被多次读取消耗 | 遵循标准模式，每次 callback 只调用一次；Identity 内置一次性 token 机制 |
