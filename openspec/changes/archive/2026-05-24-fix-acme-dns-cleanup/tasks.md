## 1. 修复 DNS 记录清理

- [x] 1.1 修改 `AcmeService.RequestCertificateAsync`：在 catch 块中调用 `CleanupAsync` 清理 DNS 记录
- [x] 1.2 修改 `AcmeService.HandleDnsChallengeAsync`：使用 try-finally 确保 DNS 记录被清理

## 2. 修复 SplitDomainName 多段 TLD

- [x] 2.1 修改 `AcmeService.SplitDomainName`：当域名段数 >= 4 时取最后 3 段为根域，否则取最后 2 段

## 3. 修复接口类型安全性

- [x] 3.1 修改 `IAcmeService` 接口中参数类型为 `IAliyunDnsService`
- [x] 3.2 修改 `AcmeService` 实现类中参数类型
- [x] 3.3 修改 `AcmeService.CleanupAsync` 的参数类型
- [x] 3.4 修改 `CertificateService` 中调用处的参数类型

## 4. 验证

- [x] 4.1 `SplitDomainName` 已标注 `internal static`，可直接在测试中调用验证
- [x] 4.2 构建通过，测试项目编译成功
