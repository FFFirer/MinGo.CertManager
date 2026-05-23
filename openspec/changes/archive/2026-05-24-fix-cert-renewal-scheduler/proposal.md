## Why

`CertificateRenewalJob` 类已经编写完成（含 `[DisallowConcurrentExecution]` 属性），但 Quartz 调度器仅在 `Program.cs` 中初始化了基础服务，没有注册任何作业和触发器。证书到期后永远不会自动续签，用户只能手动续签，导致证书过期服务中断。

## What Changes

- 在 `Program.cs` 的 Quartz 配置中注册 `CertificateRenewalJob` 并配置定时触发器
- 将从配置读取的 `RenewalDaysBeforeExpiry`（默认 30 天）作为扫描提前量
- 移除未使用的 `QuartzJobFactory.cs`（当前使用 `UseSimpleTypeLoader()` 内置加载器）

## Capabilities

### New Capabilities
- `cert-auto-renewal`: 证书自动续签调度能力，支持定期扫描即将过期的证书并自动发起续签

### Modified Capabilities

（无）

## Impact

- `src/MinGo.CertManager.Web/Program.cs` — 添加作业注册和触发器配置
- `src/MinGo.CertManager.Infrastructure/Jobs/CertificateRenewalJob.cs` — 使用配置的 `RenewalDaysBeforeExpiry` 替代硬编码的 30 天
- `src/MinGo.CertManager.Infrastructure/Quartz/QuartzJobFactory.cs` — 移除未使用的文件
