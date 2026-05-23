## Why

代码中存在多类质量问题：CS8618 可空性警告（9 个）、集成测试用 try-catch 吞异常导致测试有效性降低、CRT 下载 Content-Type 错误、Blazor 页面使用 `_ =` 丢弃异步任务、证书过期时间硬编码而非从证书解析、未使用的 `AliyunDnsValidationService` 空实现等。这些问题降低了代码质量和可维护性。

## What Changes

- 修复全部 9 个 CS8618 可空性警告
- 修改 `AcmeServiceIntegrationTests` 在断言失败时正确抛出异常
- 修复 `ExternalApiController` 中 CRT 格式的 Content-Type 为 `application/zip`
- 修复 `Certificates.razor` 中 fire-and-forget 的异步调用
- 修复证书过期时间从证书 `notAfter` 字段解析而非硬编码 90 天
- 移除未使用的 `AliyunDnsValidationService.cs` 文件
- 清理未使用的 `Authorization: ApiKey` 相关引用（在 SDK 变更中已覆盖则跳过）

## Capabilities

### New Capabilities

（无——全部是修复和清理）

### Modified Capabilities

（无）

## Impact

- `src/MinGo.CertManager.Web/Controllers/ExternalApiController.cs` — CRT Content-Type
- `src/MinGo.CertManager.Web/Pages/Certificates.razor` — `_ = LoadCertificates()` 改为 await
- `src/MinGo.CertManager.Web/Components/PageHeader.razor` — CS8618
- `src/MinGo.CertManager.Web/Pages/Users.razor` — CS8618
- `src/MinGo.CertManager.Web/Pages/Profile.razor` — CS8618
- `src/MinGo.CertManager.Web/Shared/HeaderNav.razor` — CS0649
- `src/MinGo.CertManager.Web/Controllers/ExternalApiController.cs` — CS8618
- `src/MinGo.CertManager.Application/Services/CertificateService.cs` — 从证书解析 ExpiresAt
- `src/MinGo.CertManager.Infrastructure/Services/AliyunDnsValidationService.cs` — 删除
- `test/MinGo.CertManager.Tests/Services/AcmeServiceIntegrationTests.cs` — 测试异常处理
