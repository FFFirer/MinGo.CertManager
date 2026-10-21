## Context

`CertificateRenewalJob`（Quartz，24h）扫描 `Active` 且 `ExpiresAt` 临期的证书并调用 `RenewCertificateAsync`。后者在旧记录上原地重跑 ACME：先 `Status←Pending`，成功覆盖 PEM/`ExpiresAt`，失败 `Status←Failed`。结果是失败会连带作废仍在有效期内的旧证书，且 Job 不再重试。

用户已拍板：

1. 续期改为 **创建新记录**，不复用旧记录
2. 旧记录状态 **保持正常生命周期，续期过程不主动修改**
3. 续期失败产生的记录 **保留**
4. 重试节奏 **跟随现有 24h Job**，不做额外退避

## Goals / Non-Goals

**Goals:**

- 续期落地为新 `Certificate` 行，旧证书全程可用、可导出
- 失败可随 Job 自然重试，不丢在役证书
- 防止同域名并发开单 / 已被新证书覆盖后的重复续期
- 续期血缘可追溯（`RenewedFromId` + 操作流水）
- 改动最小化：不引入通知、不引入 AutoRenew 开关、不引入退避

**Non-Goals:**

- 不把旧记录标 `Expired`/`Superseded`
- 不做退避/限速感知
- 不做站内/邮件/Webhook 通知
- 不做按证书 AutoRenew 开关
- 不改 ACME / DNS-01 流程本身
- 不改手动「续签」按钮入口

## Decisions

### 决策 1：新记录承载续期，旧记录只读

```
RenewCertificateAsync(sourceId)
  1. 读 source（须 Status==Active）
  2. 防重：同 (Domain, IsWildcard, UseStaging) 已有 Pending → 抛/跳过
  3. new Certificate {
         Id = NewGuid(),
         Domain/IsWildcard/UseStaging 复制,
         Status = Pending,
         AcmeStatus = Initializing,
         RenewedFromId = source.Id
     }
  4. 在 new 上跑 ExecuteCertificateRequestAsync
  5. 成功 → new.Status=Active；source 状态不动
     失败 → new.Status=Failed；source 状态不动；记录保留
  6. 返回 new
```

**为什么不改旧状态：** 用户明确要求「保持正常生命周期，不主动修改」。旧证书何时 `Expired` 交给时间本身（可后续单独做状态同步）；续期切换靠「同域更新的 Active 记录」而不是状态改写。

**替代方案：** 成功后把旧的标 `Expired`/`Superseded` → 语义更准但违背决策 2，且需要回滚逻辑应对失败，放弃。

### 决策 2：Job 选「当前最优」而不是所有 Active

为避免旧证书在窗口期内被反复续期（旧的始终 `Active` + 临期），选择条件为：

```
Status == Active
AND ExpiresAt <= UtcNow.AddDays(RenewalDaysBeforeExpiry)
AND 不存在同 (Domain, IsWildcard, UseStaging) 且 ExpiresAt > 本记录 的 Active 记录
AND 不存在同 (Domain, IsWildcard, UseStaging) 的 Pending 记录
```

第三条保证只续「仍是在役最优」的那张；cert-B 成功后 cert-A 自动退出候选。第四条防止 Job 与手动续签、或 Job 重入并发。

### 决策 3：失败记录保留，重试靠 Job

失败的 new 记录保持 `Failed`，不删除、不影响 source。下一轮 24h Job 会再次选中 source（仍 Active 临期且无更新后继）并新建一条再试。不做 `LastRenewAttemptAt` 退避。

### 决策 4：血缘用 `RenewedFromId`，不引入完整版本表

`Certificate.RenewedFromId`（`Guid?`）指向源证书。查询/展示够用，避免引入独立 `CertificateRevision` 表。操作流水补 `CertRenewed` / `CertRenewFailed`，消息里带上 `RenewedFromId`。

### 决策 5：最新有效证书按 `ExpiresAt` 选取

`GetLatestValidCertificateByDomainAsync` 在多条 `Active` 并存时按 `ExpiresAt` 降序（再 `IssuedAt`）取第一条，保证下载/导出 API 拿到真正在役的那张。

## Risks / Trade-offs

- **[同域多条 Active]** 旧证书不标过期会让列表出现同域多条「有效」→ UI 用 `RenewedFromId`/徽章区分；下载侧由决策 5 消歧。可接受。
- **[历史堆积]** 每次续期 + 每次失败都留行 → 表增长。可接受（证书数量级小）；后续可做清理策略。
- **[无退避重试]** DNS 持续故障时每 24h 失败一次，可能触碰 ACME 速率限制 → 决策明确不做退避；失败只开一条 `Failed` 痕迹，有限可控。
- **[旧记录永不变 Expired]** 列表状态可能长期显示「有效」直到人工/后续能力处理 → 符合用户决策，不在本次范围。

## Migration

- `Certificate` 增加 `RenewedFromId Guid?`（可空，无外键强制）
- `OperationLogAction` 枚举追加 `CertRenewed = 8` / `CertRenewFailed = 9`（若现有值更高则顺延）
- EF 迁移：单列 add
