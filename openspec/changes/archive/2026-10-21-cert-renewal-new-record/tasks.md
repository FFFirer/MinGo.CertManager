# Tasks

## 1. 数据模型与仓储

- [x] 1.1 `Certificate` 实体增加 `RenewedFromId`（`Guid?`）
- [x] 1.2 `OperationLogAction` 增加 `CertRenewed` / `CertRenewFailed`
- [x] 1.3 EF 迁移：`RenewedFromId` 列
- [x] 1.4 `ICertificateRepository` 增加：
      `GetLatestValidCertificateByDomainAsync` 改为按 `ExpiresAt` 降序；
      `GetRenewalCandidatesAsync(beforeUtc)`（Active 临期 + 无更新后继 + 无 Pending）；
      `HasPendingCertificateAsync` 覆盖续期在途

## 2. 续期服务（新建记录）

- [x] 2.1 重写 `RenewCertificateAsync`：校验源 `Active` → 创建新记录（`RenewedFromId`）→ 在新记录上跑 ACME
- [x] 2.2 成功：新记录 `Active`，源记录状态与内容不变；写 `CertRenewed` 流水
- [x] 2.3 失败：新记录 `Failed` 保留，源记录不变；写 `CertRenewFailed` 流水
- [x] 2.4 同标识已有 `Pending` 时拒绝/跳过，避免并发开单

## 3. 自动续期作业

- [x] 3.1 `CertificateRenewalJob` 改用 `GetRenewalCandidatesAsync`
- [x] 3.2 保持 24h 调度；失败不修改源证书，下轮自然重试

## 4. UI（轻量）

- [x] 4.1 `Certificates.razor` 对 `RenewedFromId` 非空的记录显示「续期」徽章

## 5. 测试

- [x] 5.1 续期成功：新建记录 + 源记录状态/内容不变
- [x] 5.2 续期失败：新记录 `Failed` 保留 + 源记录仍可导出
- [x] 5.3 防重：同标识已有 `Pending` 时不创建新记录
- [x] 5.4 候选查询：被更新后继覆盖的旧记录不再入选；无后继的临期记录入选
- [x] 5.5 最新有效证书按 `ExpiresAt` 选取
