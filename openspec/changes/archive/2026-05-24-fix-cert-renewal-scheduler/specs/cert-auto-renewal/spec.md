## ADDED Requirements

### Requirement: 定期扫描并续签即将过期的证书
系统 SHALL 定期扫描所有状态为 `Active` 且将在指定天数内过期的证书，并自动发起续签。

#### Scenario: 扫描到即将过期的证书
- **WHEN** 定时作业执行时，发现存在状态为 `Active` 且 `ExpiresAt <= DateTime.UtcNow.AddDays(RenewalDaysBeforeExpiry)` 的证书
- **THEN** 对每个符合条件的证书调用 `RenewCertificateAsync`
- **AND** 原证书状态更新为 `Expired`

#### Scenario: 续签失败时记录日志
- **WHEN** 某个证书续签过程中抛出异常
- **THEN** 系统记录错误日志，包含证书 ID 和异常详情
- **AND** 继续处理剩余证书，不中断整个作业

#### Scenario: 每日定时执行
- **WHEN** Quartz 调度器启动后
- **THEN** `CertificateRenewalJob` SHALL 每 24 小时自动执行一次
- **AND** 作业执行间隔从 `QuartzSettings` 配置读取（默认 24 小时）

### Requirement: 续签提前量可配置
证书续签的提前天数 SHALL 从 `CertificateSettings.RenewalDaysBeforeExpiry` 配置读取。

#### Scenario: 配置生效
- **WHEN** `appsettings.json` 中配置 `Certificate.RenewalDaysBeforeExpiry = 45`
- **THEN** 作业扫描时仅选择 `ExpiresAt <= DateTime.UtcNow.AddDays(45)` 的证书
