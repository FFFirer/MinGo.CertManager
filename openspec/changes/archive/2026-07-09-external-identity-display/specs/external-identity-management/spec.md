## ADDED Requirements

### Requirement: Profile 页展示已绑定外部账号

系统 SHALL 在 Profile 页展示当前用户已绑定的所有第三方外部账号。

- 列表中的每一项 SHALL 显示：
  - Provider 图标（来自对应 `IOAuthLoginProvider.IconCssClass`）
  - Provider 显示名称（来自对应 `IOAuthLoginProvider.DisplayName` 或配置覆盖值）
  - 外部标识（`ProviderKey`，如 GitHub 用户名）
- 列表 SHALL 通过 `UserManager<ApplicationUser>.GetLoginsAsync(user)` 获取绑定数据
- Provider 图标 SHALL 通过注入的 `IEnumerable<IOAuthLoginProvider>` 构建名称到图标的映射字典

#### Scenario: 用户查看已绑定外部账号
- **WHEN** 用户访问 Profile 页且已绑定 1 个或多个外部账号
- **THEN** 系统 SHALL 在"第三方账号"区块中展示所有已绑定账号的 Provider 图标、名称和外部标识
- **WHEN** 用户未绑定任何外部账号
- **THEN** 系统 SHALL 显示"尚未绑定第三方账号"

### Requirement: Profile 页解绑外部账号

系统 SHALL 允许用户在 Profile 页解绑已绑定的外部账号。

- 每个已绑定的外部账号 SHALL 显示"解除绑定"按钮
- 解绑前 SHALL 弹出确认提示
- 解绑 SHALL 调用 `UserManager.RemoveLoginAsync(user, loginProvider, providerKey)`
- 解绑成功后 SHALL 调用 `SignInManager.RefreshSignInAsync(user)` 刷新登录 cookie

#### Scenario: 用户成功解绑外部账号
- **WHEN** 用户点击某外部账号的"解除绑定"按钮并确认
- **THEN** 系统 SHALL 调用 `RemoveLoginAsync` 移除绑定关系
- **THEN** 系统 SHALL 刷新登录 cookie
- **THEN** 该外部账号 SHALL 从列表中移除

#### Scenario: 解绑安全约束阻止解绑
- **WHEN** 用户无本地密码且仅剩 1 个已绑定的外部账号
- **THEN** 系统 SHALL 禁用该外部账号的"解除绑定"按钮
- **THEN** 系统 SHALL 显示"不可解除"提示
- **WHEN** 用户有本地密码或绑定了多个外部账号
- **THEN** 系统 SHALL 允许解绑

### Requirement: Profile 页绑定新外部账号

系统 SHALL 允许用户在 Profile 页绑定新的外部账号。

- 系统 SHALL 展示当前已启用（在 `EnabledProviders` 配置中）且尚未绑定的 Provider 列表
- 每个可绑定的 Provider SHALL 显示为按钮（含 Provider 图标和名称）
- 用户点击 Provider 按钮后，SHALL POST 到 `ExternalLogin` PageModel 的 `LinkLogin` handler
- `LinkLogin` handler SHALL 发起 OAuth Challenge，回调地址指向 `LinkLoginCallback`
- `LinkLoginCallback` handler SHALL 通过 `GetExternalLoginInfoAsync()` 获取外部登录信息
- `LinkLoginCallback` SHALL 调用 `UserManager.AddLoginAsync(user, externalLoginInfo)` 完成绑定
- 绑定成功后 SHALL 调用 `SignInManager.RefreshSignInAsync(user)` 刷新登录 cookie
- 绑定完成后 SHALL 重定向回 Profile 页

#### Scenario: 用户成功绑定新外部账号
- **WHEN** 用户在 Profile 页点击一个未绑定的 Provider 按钮
- **THEN** 系统 SHALL POST 到 ExternalLogin LinkLogin handler
- **THEN** 系统 SHALL 发起 OAuth Challenge 重定向到 Provider
- **WHEN** 用户在 Provider 侧授权后回调
- **THEN** 系统 SHALL 调用 `AddLoginAsync` 绑定到当前用户
- **THEN** 系统 SHALL 刷新 cookie 并重定向到 Profile 页
- **THEN** 该 Provider SHALL 出现在已绑定列表中

#### Scenario: 已绑定账号不可重复绑定
- **WHEN** 用户已绑定了某 Provider
- **THEN** 该 Provider SHALL 不出现在可绑定的 Provider 列表中

### Requirement: Users 页展示用户外部身份

系统 SHALL 在 Users 页（管理员用户列表）展示每个用户绑定的外部身份。

- 表格 SHALL 新增"外部身份"列
- 每个绑定的 Provider SHALL 以徽标（badge）形式展示
- 徽标 SHALL 显示 `UserLoginInfo.ProviderDisplayName`（如 "GitHub"）
- 不同 Provider SHALL 使用不同颜色徽标区分
- 未绑定外部账号的用户 SHALL 显示"—"
- 数据 SHALL 通过每用户调用 `UserManager<ApplicationUser>.GetLoginsAsync(user)` 获取

#### Scenario: 管理员查看用户外部身份
- **WHEN** 管理员访问 Users 页
- **THEN** 系统 SHALL 在表格中为每个用户显示"外部身份"列
- **WHEN** 用户绑定了 GitHub
- **THEN** 系统 SHALL 在对应行显示 GitHub 徽标
- **WHEN** 用户绑定了多个 Provider
- **THEN** 系统 SHALL 显示多个徽标
- **WHEN** 用户未绑定任何外部账号
- **THEN** 系统 SHALL 显示"—"
