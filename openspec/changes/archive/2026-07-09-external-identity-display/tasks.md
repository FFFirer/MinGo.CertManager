## 1. ExternalLogin PageModel — 新增绑定链路

- [x] 1.1 在 `ExternalLogin.cshtml.cs` 新增 `OnPostLinkLogin` handler — 发起 OAuth Challenge，回调地址指向 LinkLoginCallback
- [x] 1.2 在 `ExternalLogin.cshtml.cs` 新增 `OnGetLinkLoginCallbackAsync` handler — 检测当前用户已登录、获取 ExternalLoginInfo、调用 AddLoginAsync、RefreshSignInAsync
- [x] 1.3 验证：新 handler 不破坏现有登录/回调流程（不修改已有代码路径）

## 2. Profile 页 — 第三方账号管理区块

- [x] 2.1 在 `Profile.razor` 注入 `UserManager<ApplicationUser>`（如已有则跳过）和 `IEnumerable<IOAuthLoginProvider>`
- [x] 2.2 新增 `LoadExternalLoginsAsync` 方法，调用 `UserManager.GetLoginsAsync(user)` 获取已绑定外部账号
- [x] 2.3 构建 Provider 名称→图标映射字典（从 `IOAuthLoginProvider` 列表）
- [x] 2.4 新增"第三方账号" UI 区块 — 展示已绑定外部账号列表（Provider 图标+名称+标识、解绑按钮）
- [x] 2.5 实现解绑逻辑：确认对话框 → `RemoveLoginAsync` → `RefreshSignInAsync` → 刷新列表
- [x] 2.6 实现解绑安全约束：无密码且仅剩 1 个外部账号时禁用解绑
- [x] 2.7 新增"绑定第三方账号"子区块 — 展示已启用且未绑定的 Provider 按钮列表（form POST 到 LinkLogin handler）

## 3. Users 页 — 外部身份徽标列

- [x] 3.1 在 `Users.razor` 新增 `userLogins` 字典字段和 `LoadUserLoginsAsync` 方法
- [x] 3.2 表格新增"外部身份"列 — 循环展示用户绑定的每个 Provider 的徽标（badge），无绑定时显示"—"
- [x] 3.3 为不同 Provider 定义不同徽标颜色（如 GitHub→灰色，SimpleIdServer→蓝色）
