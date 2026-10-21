## Why

当前续期走「原地复用」：`RenewCertificateAsync` 先把旧证书置为 `Pending` 再跑 ACME，失败则整条记录变 `Failed`。一张还有数十天有效期的证书会因一次瞬时 DNS 失败变成不可导出、不可下载、且不再被自动重试（Job 只扫 `Active`）。需要把续期改为「新建记录」，让旧证书完整保留、失败可重试。

## What Changes

- **BREAKING** `RenewCertificateAsync` 不再复用旧记录：改为创建新的 `Certificate` 记录并在其上完成 ACME 流程
- 旧记录 **状态保持原有生命周期，续期过程不主动修改**（不标 `Expired` / `Superseded`）
- 续期产生的新记录失败时 **保留**（`Failed`），仅作痕迹，不影响旧证书
- 新记录写入 `RenewedFromId` 指向源证书，形成续期血缘链
- `CertificateRenewalJob` 继续每 24h 扫描；选择条件收紧为「当前最优」的 `Active` 临期证书，避免对已被更新证书覆盖的旧记录重复开单
- 同域名（含 `IsWildcard` / `UseStaging`）已存在 `Pending` 记录时跳过，防止手动+自动并发续期
- 成功/失败写入操作流水（`CertRenewed` / `CertRenewFailed`）
- `GetLatestValidCertificateByDomainAsync` 改为按 `ExpiresAt` 取最新在役证书
- UI 续期入口行为不变（仍调 `RenewCertificateAsync`），列表可见续期血缘

## Capabilities

### New Capabilities

- `cert-renewal-new-record`: 续期以新建证书记录落地，旧记录状态不被续期过程改写，失败记录保留并随 Job 每 24h 重试

### Modified Capabilities

- （归档）`cert-auto-renewal`: 扫描条件从「所有 Active 临期」收紧为「无更新后继、无 Pending 在途的 Active 临期」；续期不再改写原证书状态

## Impact

- **Core**：`Certificate` 实体 + `RenewedFromId`；`OperationLogAction` + `CertRenewed` / `CertRenewFailed`
- **Application**：`CertificateService.RenewCertificateAsync` 重写为新建记录流程；选择/防重逻辑
- **Infrastructure**：`CertificateRepository` 查询（最新有效 / 临期待续 / 同域 Pending）；EF 迁移加列
- **Web**：`Certificates.razor` 血缘展示（轻量）；续期按钮不变
- **Jobs**：`CertificateRenewalJob` 调用新查询，失败不吞旧证书
- **测试**：新建续期、失败保旧、防重复开单、最新有效选取
