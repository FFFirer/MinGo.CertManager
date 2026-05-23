## Why

ACME 证书申请流程中存在两个 bug：1) 申请失败时不会清理已创建的阿里云 DNS TXT 记录，导致残留记录可能影响后续申请；2) `SplitDomainName` 方法硬编码取域名最后两段作为根域，对于 `co.uk`、`com.cn` 等多段 TLD 域名解析错误，导致 DNS 记录操作在错误的域上执行。

## What Changes

- `AcmeService.RequestCertificateAsync` catch 分支调用 `CleanupAsync` 清理 DNS 记录
- 将 `AcmeService` 中 `dnsService` 参数类型从 `object` 改为 `IAliyunDnsService`，消除运行时转换风险
- 修复 `SplitDomainName` 使用公共后缀列表识别根域，或者改用简单启发式（至少三段时才取最后两段）

## Capabilities

### New Capabilities
- `acme-error-recovery`: ACME 流程失败时的资源清理与错误恢复能力

### Modified Capabilities

（无）

## Impact

- `src/MinGo.CertManager.Application/Services/AcmeService.cs`
- `src/MinGo.CertManager.Core/Services/IAcmeService.cs` — `dnsService` 参数类型从 `object` 变为 `IAliyunDnsService`
- `src/MinGo.CertManager.Application/Services/CertificateService.cs` — 调用 `RequestCertificateAsync` 处需适配签名变更
