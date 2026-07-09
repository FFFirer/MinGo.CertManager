## ADDED Requirements

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

## MODIFIED Requirements

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
