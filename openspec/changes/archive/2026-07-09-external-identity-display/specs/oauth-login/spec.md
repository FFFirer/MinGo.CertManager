## ADDED Requirements

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
