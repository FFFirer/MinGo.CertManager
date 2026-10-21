## ADDED Requirements

### Requirement: 续期以新建证书记录落地

系统 SHALL 在证书续期时创建新的 `Certificate` 记录并在新记录上完成 ACME 流程，SHALL NOT 在源证书记录上原地重跑申请。

- 新记录 SHALL 复制源证书的 `Domain`、`IsWildcard`、`UseStaging`
- 新记录 SHALL 以 `RenewedFromId` 指向源证书
- 新记录初始状态 SHALL 为 `Pending`，ACME 过程状态 SHALL 为 `Initializing`
- 仅当源证书状态为 `Active`（已申请成功）时才允许发起续期

#### Scenario: 续期成功创建新记录

- **WHEN** 对状态为 `Active` 的证书发起续期且 ACME 流程成功
- **THEN** 系统 SHALL 创建新的证书记录，写入新的证书内容、私钥、证书链、签发时间与过期时间
- **AND** 新记录状态 SHALL 为 `Active`
- **AND** 源证书记录 SHALL 保持原有状态与证书内容不变

#### Scenario: 续期失败保留痕迹且不影响源证书

- **WHEN** 续期过程中 ACME 流程失败
- **THEN** 新记录状态 SHALL 为 `Failed` 且 SHALL 保留（不删除）
- **AND** 源证书记录 SHALL 保持原有状态与证书内容不变
- **AND** 源证书 SHALL 继续可导出、可下载

#### Scenario: 非 Active 证书禁止续期

- **WHEN** 对状态不为 `Active` 的证书发起续期
- **THEN** 系统 SHALL 拒绝续期并返回错误

### Requirement: 续期防并发与防重复开单

系统 SHALL 避免同一证书标识（`Domain` + `IsWildcard` + `UseStaging`）下出现并发或重复的续期/申请在途记录。

#### Scenario: 已有 Pending 记录时跳过续期

- **WHEN** 同一证书标识下已存在状态为 `Pending` 的记录
- **THEN** 系统 SHALL NOT 为该标识再创建续期记录
- **AND** 自动续期作业 SHALL 跳过该源证书

#### Scenario: 手动与自动续期不并发

- **WHEN** 自动续期作业扫描时发现某证书标识已有 `Pending` 在途记录
- **THEN** 作业 SHALL 跳过该证书，不调用续期

### Requirement: 自动续期仅覆盖当前在役最优证书

自动续期作业 SHALL 只对「仍为当前在役最优」的临期证书发起续期，避免对已被更新证书覆盖的历史记录重复续期。

源证书候选条件 SHALL 同时满足：

- 状态为 `Active`
- `ExpiresAt <= UtcNow + RenewalDaysBeforeExpiry`
- 不存在同证书标识下 `ExpiresAt` 更晚的 `Active` 记录
- 不存在同证书标识下的 `Pending` 记录

#### Scenario: 已被更新证书覆盖后不再续期

- **WHEN** 证书 A 临期，但同标识下已存在 `ExpiresAt` 更晚的 `Active` 证书 B
- **THEN** 自动续期作业 SHALL NOT 对证书 A 发起续期

#### Scenario: 当前最优临期证书被自动续期

- **WHEN** 证书 C 为同标识下 `ExpiresAt` 最晚的 `Active` 记录且已进入续期提前量
- **THEN** 自动续期作业 SHALL 为其创建续期记录并发起 ACME 流程

### Requirement: 续期重试跟随每日作业

续期失败后系统 SHALL 随现有 24 小时自动续期作业自然重试，SHALL NOT 因失败而将源证书移出候选集合。

#### Scenario: 失败后下轮作业重试

- **WHEN** 某源证书上一次自动续期失败（产生 `Failed` 记录）且源证书仍为在役最优临期 `Active`
- **THEN** 下一轮作业 SHALL 再次为该源证书创建新的续期记录

#### Scenario: 作业每日执行

- **WHEN** Quartz 调度器运行中
- **THEN** `CertificateRenewalJob` SHALL 每 24 小时执行一次

### Requirement: 续期提前量可配置

续期提前天数 SHALL 从 `CertificateSettings.RenewalDaysBeforeExpiry` 读取。

#### Scenario: 配置生效

- **WHEN** 配置 `RenewalDaysBeforeExpiry = 45`
- **THEN** 作业 SHALL 仅选择 `ExpiresAt <= UtcNow.AddDays(45)` 的候选证书

### Requirement: 最新有效证书选取

按域名获取最新有效证书时，系统 SHALL 在多条 `Active` 并存时返回 `ExpiresAt` 最晚的一条。

#### Scenario: 多条 Active 时取过期时间最晚者

- **WHEN** 同域名存在两条 `Active` 记录且过期时间不同
- **THEN** `GetLatestValidCertificateByDomainAsync` SHALL 返回 `ExpiresAt` 最晚的记录
