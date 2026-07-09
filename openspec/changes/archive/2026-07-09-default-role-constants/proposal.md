## Why

目前角色名（"Admin"、"User"）在代码中以硬编码字符串形式散落在多处，既容易拼错，也无法集中管理。同时，"User" 角色需要通过显式调用 `AddToRoleAsync(user, "User")` 来赋予，而不是作为用户的默认角色自动生效。这导致代码冗余且容易遗漏——如果未来新增用户创建路径，可能忘记赋予默认角色。

## What Changes

1. **提取角色常量**：创建 `RoleConstants` 类，统一定义 `"Admin"` 和 `"User"` 角色名，替换所有硬编码引用。
2. **默认角色机制**：通过 ASP.NET Identity 的扩展机制，使新创建的 `ApplicationUser` 自动获得 "User" 角色，无需在每个创建处显式调用 `AddToRoleAsync`。
3. **清理冗余代码**：`ExternalLogin.cshtml.cs` 中不再需要手动调用 `AddToRoleAsync(user, "User")`，移除这些调用。
4. **更新种子服务**：`IdentitySeedService.cs` 中角色创建使用常量。

## Capabilities

### New Capabilities

- `role-management`: 角色名集中定义和默认角色机制，确保所有新增用户自动获得 "User" 角色

### Modified Capabilities

- `oauth-login`: "无匹配账号自动创建" Requirement 中，新用户的角色赋予方式从显式调用改为默认机制实现

## Impact

- **New file**: `MinGo.CertManager.Core/Constants/RoleConstants.cs` — 角色名常量定义
- **Modified**: `MinGo.CertManager.Infrastructure/Services/IdentitySeedService.cs` — 使用角色常量
- **Modified**: `MinGo.CertManager.Web/Pages/ExternalLogin.cshtml.cs` — 移除显式 `AddToRoleAsync("User")` 调用
- **Modified**: `MinGo.CertManager.Infrastructure/Data/ApplicationUser.cs` — 可选：添加默认角色相关逻辑
- **New/Modified**: 自定义 `UserClaimsPrincipalFactory` 或 `IUserStore` 扩展以确保创建时自动赋角色
- **Modified**: `openspec/specs/oauth-login/spec.md` — 更新 Requirement: 无匹配账号自动创建
