## Context

当前系统中角色名（"Admin"、"User"）以硬编码字符串形式出现在 `IdentitySeedService.cs` 和 `ExternalLogin.cshtml.cs` 中。新用户通过外部登录创建时，需显式调用 `await _userManager.AddToRoleAsync(user, "User")` 才能赋予角色。

这种方式存在两个问题：
1. 字符串散落各处，容易拼写错误
2. 新增用户创建路径时容易遗漏角色赋予

系统使用 ASP.NET Core Identity，`UserManager<ApplicationUser>` 是用户管理的核心入口。所有用户创建都经过 `UserManager.CreateAsync`。

## Goals / Non-Goals

**Goals:**
- 角色名统一为常量定义，消除所有硬编码字符串
- 新创建的 `ApplicationUser` 自动获得 "User" 角色，无需在调用方显式添加
- 保持向后兼容，现有用户角色不受影响
- 所有现有功能保持不变

**Non-Goals:**
- 不修改数据库架构
- 不改动 Admin 用户的创建逻辑——种子服务仍显式赋予 "Admin" 角色
- 不引入新的 NuGet 包
- 不改动现有用户的角色数据

## Decisions

### Decision 1: 角色常量位置 — 新增 `Core/Constants/RoleConstants.cs`

**方案**: 在 `MinGo.CertManager.Core` 层新增 `Constants` 目录，创建 `RoleConstants` 静态类。

```csharp
namespace MinGo.CertManager.Core.Constants;

public static class RoleConstants
{
    public const string Admin = "Admin";
    public const string User = "User";
}
```

**理由**: Core 层被所有上层项目引用，角色常量放在这里可以被所有层访问。const 字符串编译时内联，无运行时开销。

### Decision 2: 默认角色机制 — 自定义 `ApplicationUserManager` 重写 `CreateAsync`

**方案**: 继承 `UserManager<ApplicationUser>` 并重写 `CreateAsync`，在用户创建成功后自动赋予 "User" 角色。

```csharp
public class ApplicationUserManager : UserManager<ApplicationUser>
{
    // 完整构造函数委托给基类

    public override async Task<IdentityResult> CreateAsync(ApplicationUser user)
    {
        var result = await base.CreateAsync(user);
        if (result.Succeeded)
        {
            await AddToRoleAsync(user, RoleConstants.User);
        }
        return result;
    }
}
```

在 `Program.cs` 中替换默认注册：
```csharp
builder.Services.AddScoped<UserManager<ApplicationUser>, ApplicationUserManager>();
```

**影响分析—Admin 用户**: 种子服务创建 Admin 用户时也会经过 `CreateAsync`，导致 Admin 先获得 "User" 角色，然后种子服务再添加 "Admin" 角色。最终 Admin 用户同时拥有 "User" 和 "Admin" 两个角色。这是可接受的——Admin 本身就是 User + 管理权限。无需特殊处理。

**理由**: 
- 最彻底的方案——无论未来新增任何用户创建路径，都自动获得 "User" 角色
- `CreateAsync` 是 Identity 官方推荐的扩展点（virtual 方法）
- 无需修改数据库架构
- 无需在调用方做任何特殊处理

**被否掉的替代方案**:
- `IUserClaimsPrincipalFactory` — 只影响登录后的 ClaimsPrincipal，不创建 `AspNetUserRoles` 表记录
- Entity 默认值 — 角色存储在单独的关联表中，不适合实体属性
- `DbContext.SaveChangesAsync` 拦截 — 横切关注点过于宽泛

### Decision 3: 清理外部登录中的显式角色调用

`ExternalLogin.cshtml.cs` 中两处 `await _userManager.AddToRoleAsync(user, "User")` 将被移除，因为 `ApplicationUserManager.CreateAsync` 已自动处理。

| 位置 | 当前代码 | 改为 |
|------|---------|------|
| `OnGetCallbackAsync` (L173) | `AddToRoleAsync(user, "User")` | 删除此行 |
| `OnPostCompleteEmailAsync` (L269) | `AddToRoleAsync(user, "User")` | 删除此行 |

### Decision 4: IdentitySeedService 使用角色常量

`IdentitySeedService.cs` 中的 `"Admin"` 和 `"User"` 字符串替换为 `RoleConstants.Admin` 和 `RoleConstants.User`。

## Risks / Trade-offs

- **[构造兼容]** 自定义 UserManager 的构造函数参数表与基类一致，如果微软在 .NET 更新中修改构造函数签名，需要同步更新。→ **缓解**: Identity 的构造函数模式多年未变，变化风险极低。
- **[双重角色]** Admin 用户自动获得 "User" 角色，种子服务再添加 "Admin" 角色。→ **接受**: Admin 本来就是 User 的超集，两个角色并存符合 RBAC 常见实践。
- **[测试影响]** 现有测试如果依赖用户创建后不自动带有角色，需要更新断言。→ **缓解**: 应该没有这样的测试，且自动赋角色是预期行为。

## Migration Plan

1. 创建 `RoleConstants.cs`
2. 创建 `ApplicationUserManager.cs`
3. 在 `Program.cs` 中注册自定义 UserManager
4. 更新 `IdentitySeedService.cs` 使用常量
5. 清理 `ExternalLogin.cshtml.cs` 中的显式角色调用
6. 更新 spec 文档
7. 构建并验证

