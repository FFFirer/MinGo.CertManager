## 1. 角色常量定义

- [x] 1.1 在 `MinGo.CertManager.Core` 层创建 `Constants/RoleConstants.cs` 静态类，定义 `Admin` 和 `User` 常量

## 2. 自定义 ApplicationUserManager

- [x] 2.1 在 `MinGo.CertManager.Infrastructure` 层创建 `Services/ApplicationUserManager.cs`，继承 `UserManager<ApplicationUser>`，重写 `CreateAsync` 在用户创建后自动添加 "User" 角色
- [x] 2.2 在 `Program.cs` 中将 `UserManager<ApplicationUser>` 的默认注册替换为 `ApplicationUserManager`

## 3. 更新现有代码使用常量

- [x] 3.1 在 `IdentitySeedService.cs` 中将 `"Admin"` 和 `"User"` 硬编码字符串替换为 `RoleConstants.Admin` 和 `RoleConstants.User`
- [x] 3.2 在 `ExternalLogin.cshtml.cs` 中移除两处 `AddToRoleAsync(user, "User")` 调用（第 173 行和第 269 行），因为 `ApplicationUserManager` 已自动处理

## 4. 验证

- [x] 4.1 执行 `dotnet build` 确认编译通过
- [x] 4.2 执行 `dotnet test` 确认测试通过
