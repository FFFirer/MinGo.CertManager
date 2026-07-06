## ADDED Requirements

### Requirement: 无编译警告
项目代码在构建时 SHALL 不产生 CS8618、CS0649 等可空性编译警告。

#### Scenario: 构建无警告
- **WHEN** 执行 `dotnet build`
- **THEN** 所有项目构建成功且无 CS8618、CS0649 警告

### Requirement: 测试异常应正确报告
单元测试和集成测试 SHALL 在断言失败时抛出异常，而非静默捕获。

#### Scenario: 测试失败应抛出
- **WHEN** 测试中的断言条件不满足
- **THEN** 测试框架收到失败信号，测试结果标记为失败

### Requirement: 证书过期时间从证书解析
系统 SHALL 从证书内容的 `notAfter` 字段解析过期时间，而非使用固定天数配置。

#### Scenario: 证书解析成功
- **WHEN** 证书申请完成且 `CertificateContent` 包含有效的 PEM 格式证书
- **THEN** 系统使用 BouncyCastle 解析 `NotAfter` 字段作为 `ExpiresAt` 值

#### Scenario: 证书解析失败
- **WHEN** 证书内容为空或格式无效，无法解析
- **THEN** 系统使用配置的 `ValidityDays` 值计算过期时间
- **AND** 记录警告日志
