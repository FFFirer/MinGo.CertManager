## Why

系统已支持 GitHub、SimpleIdServer 等第三方 OAuth/OIDC 登录，用户可以通过外部身份登录系统。但无论是对用户自己还是管理员，都看不到每个账号绑定了哪些外部身份，也无法在界面上管理这些绑定关系。用户需要一个入口来查看和管理自己的外部账号绑定，管理员也需要了解用户的认证方式。

## What Changes

- **Profile 页新增"第三方账号"管理区块**：显示当前用户已绑定的外部账号列表（Provider 图标 + 名称 + 外部标识），支持解绑和绑定新的外部账号
- **Users 页新增"外部身份"列**：管理员可以在用户列表中看到每个用户绑定了哪些外部 Provider，以徽标形式展示
- **ExternalLogin PageModel 新增绑定链路**：新增 `LinkLogin`/`LinkLoginCallback` handler，与现有登录流程分离，已登录用户通过 OAuth Challenge 绑定新外部账号

## Capabilities

### New Capabilities
- `external-identity-management`: 用户在 Profile 页查看、绑定、解绑第三方外部账号的功能

### Modified Capabilities
- `oauth-login`: ExternalLogin 页面新增 LinkLogin/LinkLoginCallback handler，支持已登录用户绑定外部账号

## Impact

- `Pages/ExternalLogin.cshtml.cs` — 新增 2 个 handler（LinkLogin, LinkLoginCallback），不修改现有 Callback 逻辑
- `Pages/Profile.razor` — 新增"第三方账号"区块
- `Pages/Users.razor` — 用户列表新增"外部身份"列
- 无数据库变更（复用 AspNetUserLogins 表）
- 无新增 NuGet 包
- 无新增依赖
