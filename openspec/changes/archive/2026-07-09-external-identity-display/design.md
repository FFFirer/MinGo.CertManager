## Context

系统已支持通过 GitHub、SimpleIdServer 等第三方 OAuth/OIDC Provider 登录，使用 `IOAuthLoginProvider` 插件体系和 `AspNetUserLogins` 表存储绑定关系。但用户和管理员均无法在界面上查看或管理这些绑定：

- **Profile 页**：仅提供密码修改功能，无外部账号管理入口
- **Users 页**：用户列表仅显示用户名、角色、创建时间，无外部身份信息
- **绑定只能通过登录流程自动完成**，没有用户主动绑定/解绑的途径

## Goals / Non-Goals

**Goals:**
- 用户在 Profile 页查看已绑定的外部账号（Provider 图标 + 名称 + 外部标识）
- 用户在 Profile 页解绑外部账号（安全约束：无密码且只剩一个外部账号时不可解绑）
- 用户在 Profile 页绑定新的外部账号（展示已启用但未绑定的 Provider 列表）
- 管理员在 Users 页看到每个用户绑定了哪些外部 Provider（徽标展示）
- 绑定流程复用现有 ExternalLogin OAuth Challenge 机制，不与登录流程耦合

**Non-Goals:**
- 不新增数据库表（复用 AspNetUserLogins）
- 不新增 NuGet 包
- 不修改现有 login/external login 流程
- 不支持管理员代为绑定外部账号（仅用户自服务）
- 不支持外部账号显示名称自定义编辑

## Decisions

### 1. 绑定流程：独立 Handler vs 复用现有 Callback

**选择：独立 LinkLogin/LinkLoginCallback Handler**

- 现有 `OnGetCallbackAsync` 处理首次登录自动绑定/创建账号
- 新增 `OnPostLinkLogin`（Challenge 发起）+ `OnGetLinkLoginCallbackAsync`（回调绑定）
- 两个流程完全分离，互不干扰，降低回归风险
- 已登录用户走 LinkLoginCallback → `UserManager.AddLoginAsync` → `RefreshSignInAsync`
- 未登录用户走现有 Callback → `ExternalLoginSignInAsync` / `AddLoginAsync` + `SignInAsync`

### 2. 绑定链路：SignInScheme 一致性

绑定回调依赖 `SignInManager.GetExternalLoginInfoAsync()` 读取临时 external cookie。此 cookie 由 OAuth 流程完成时写入。现有配置已设置 `SignInScheme = IdentityConstants.ExternalScheme`，绑定回调可直接复用。

### 3. 解绑安全约束

- 用户有密码 → 始终可解绑
- 用户无密码 && 绑定了 2+ 个外部账号 → 可解绑
- 用户无密码 && 只剩 1 个外部账号 → 禁止解绑（防止无法登录）
- 管理员不受此限制（仅在 Users 页按需展示）

### 4. Users 页数据加载策略

当前 Users 页使用 `UserManager.Users.ToList()` + 每用户 `GetRolesAsync`。新增外部身份显示后，每用户额外调用 `GetLoginsAsync`。对于用户数较少的证书管理系统（通常 < 100 用户），N+1 查询在可接受范围内。

### 5. Provider 图标映射

Profile 页需要将 `UserLoginInfo.LoginProvider`（如 "github"）映射到 `IOAuthLoginProvider.IconCssClass`（如 "fab fa-github"）。方案：在 Web 层注入 `IEnumerable<IOAuthLoginProvider>` 并构建字典 `providerName → IconCssClass`。

## Risks / Trade-offs

- **[风险] 绑定回调时 external cookie 过期**：现有配置 `ExpireTimeSpan = 5 分钟`，用户在 Profile 页点击绑定后若在 Provider 侧停留过久可能导致 cookie 失效。**缓解措施**：保持 5 分钟配置，OAuth 流程通常很快；若发生则提示用户重试
- **[风险] Provider 已绑定到其他账号时 AddLoginAsync 失败**：Identity 约束每个 `(LoginProvider, ProviderKey)` 组合唯一。**缓解措施**：捕获失败结果，在 UI 提示"该外部账号已被其他用户绑定"
- **[风险] N+1 查询**：Users 页每用户多一次 GetLoginsAsync 查询。**缓解措施**：用户量小的管理工具可接受；后续可优化为单次 `Join` 查询
- **[取舍] 不引入全新页面**：外部账号管理直接嵌入 Profile.razor，不新增独立页面，符合"最小改动"原则
