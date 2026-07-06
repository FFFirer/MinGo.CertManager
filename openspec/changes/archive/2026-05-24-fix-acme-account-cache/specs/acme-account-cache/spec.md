## ADDED Requirements

### Requirement: ACME 缓存账号恢复
系统在证书申请时，若本地已有缓存的 ACME 账号信息，SHALL 使用缓存的账号密钥恢复已有账号，而非创建新账号。

#### Scenario: 缓存命中时复用已有账号
- **WHEN** `AcmeService.RequestCertificateAsync` 被调用，且本地缓存存在匹配的 `AcmeServerUrl` 与 `contact` 的账号记录
- **THEN** 系统使用缓存的 `AccountKey` 通过 `AcmeContext(uri, key)` + `Account()` 恢复已有账号
- **AND** 不调用 `NewAccount()` 方法
- **AND** 更新账号的 `LastUsedAt` 字段

#### Scenario: 缓存未命中时创建新账号
- **WHEN** `AcmeService.RequestCertificateAsync` 被调用，且本地缓存不存在匹配的账号记录
- **THEN** 系统调用 `NewAccount()` 创建新 ACME 账号
- **AND** 将新账号的 `AccountKey` 缓存到本地数据库

#### Scenario: 恢复失败时报错
- **WHEN** `Account()` 因网络问题或账号已被吊销而抛出异常
- **THEN** 系统记录错误日志
- **AND** 抛出异常，由上层决定是否重试或切换到创建新账号
