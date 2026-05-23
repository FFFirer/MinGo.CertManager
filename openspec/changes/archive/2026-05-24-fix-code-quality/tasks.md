## 1. 修复编译警告

- [x] 1.1 修复 `ExternalApiController.CertificateRequest.Domain` CS8618：添加 `= string.Empty`
- [x] 1.2 修复 `PageHeader.razor` 中 `Description` 和 `Actions` CS8618
- [x] 1.3 修复 `Users.razor` 中 `UserManager`/`RoleManager` CS8618：添加 `= null!`
- [x] 1.4 修复 `Profile.razor` 中 `UserManager`/`NavigationManager`/`currentUser` CS8618
- [x] 1.5 修复 `HeaderNav.razor` 中 `module` 字段 CS0649：添加 `= null`

## 2. 修复测试吞异常

- [x] 2.1 修改 `AcmeServiceIntegrationTests` 中所有 catch 块：移除 try-catch 或用 `Assert.ThrowsAsync` 代替
- [x] 2.2 为集成测试添加 `[Trait("Category", "Integration")]` 标记

## 3. 修复运行时问题

- [x] 3.1 修改 `ExternalApiController.DownloadLatestCertificate` 中 CRT 的 Content-Type 为 `application/zip`
- [x] 3.2 修改 `Certificates.razor` 中 `OnFilterStatusChange` 和 `OnSearchDomainChange`：将 `_ = LoadCertificates()` 改为 `await LoadCertificates()`

## 4. 修复证书过期时间逻辑

- [x] 4.1 添加 `ParseExpiryDate` 从 PEM 证书解析 `NotAfter` 作为 `ExpiresAt`
- [x] 4.2 解析失败时回退到 `CertificateSettings.ValidityDays`

## 5. 清理死代码

- [x] 5.1 删除 `src/MinGo.CertManager.Infrastructure/Services/AliyunDnsValidationService.cs`
- [x] 5.2 从 `Program.cs` 移除 `IDnsValidationService` 的 DI 注册

## 6. 验证

- [x] 6.1 执行 `dotnet build`：0 个警告，0 个错误 ✅
- [ ] 6.2 执行 `dotnet test` 确认测试通过（需确认预置失败）
