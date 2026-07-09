## 1. Core 层：扩展 IOAuthLoginProvider 接口

- [x] 1.1 在 `MinGo.CertManager.Core/Services/IOAuthLoginProvider.cs` 中添加 `AuthenticationType` 枚举和 `AuthType` 默认属性

## 2. Infrastructure 层：新增 SimpleIdServer Provider

- [x] 2.1 新增 `MinGo.CertManager.Infrastructure/Services/SimpleIdServerOAuthLoginProvider.cs`，实现 `IOAuthLoginProvider`，设置 AuthType = OpenIdConnect

## 3. Web 层：扩展 OAuthProviderExtensions

- [x] 3.1 在 `OAuthProviderExtensions.cs` 中新增 `RegisterOpenIdConnectProvider` 私有方法，使用 `AddOpenIdConnect` 注册 OIDC 方案
- [x] 3.2 修改 `RegisterOAuthProvider` 分流逻辑：根据 `instance.AuthType` 调用 `RegisterOAuthProvider` 或 `RegisterOpenIdConnectProvider`
- [x] 3.3 在 `RegisterOAuthProvider` 和 `RegisterOpenIdConnectProvider` 中增加 DisplayName 配置读取逻辑：`var displayName = section["DisplayName"] ?? instance.DisplayName`
- [x] 3.4 修改注册循环，按 `EnabledProviders` 数组顺序注册 Provider，去掉对未启用 Provider 的注册

## 4. Web 层：修改 Login.cshtml 排序

- [x] 4.1 修改 `Login.cshtml`，去掉 `.OrderBy(p => p.DisplayOrder)`，直接使用注入的 `IEnumerable<IOAuthLoginProvider>` 顺序

## 5. Web 层：配置文件更新

- [x] 5.1 在 `appsettings.json` 中添加 `simpleidserver` 配置节，加入 `EnabledProviders` 数组
- [x] 5.2 添加开发调试用的 User Secrets 占位说明

## 6. 验证

- [x] 6.1 确认编译通过，无 LSP 错误
- [x] 6.2 确认 GitHub/Google Provider 功能不受影响（向后兼容）
- [x] 6.3 确认 SimpleIdServer 按钮按 `EnabledProviders` 数组顺序显示
- [x] 6.4 确认 DisplayName 配置覆盖生效
- [x] 6.5 确认 dotnet build 通过
