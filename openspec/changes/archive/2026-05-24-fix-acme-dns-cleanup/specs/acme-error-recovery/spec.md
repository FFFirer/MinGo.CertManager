## ADDED Requirements

### Requirement: ACME 失败时清理 DNS 记录
证书申请流程中，若 DNS TXT 记录已创建但后续步骤失败，系统 SHALL 自动清理已创建的 DNS 记录。

#### Scenario: DNS 验证失败时清理记录
- **WHEN** DNS TXT 记录已成功创建但后续挑战验证失败
- **THEN** 系统调用 `ClearTxtRecordAsync` 删除已创建的 DNS TXT 记录
- **AND** 抛出原始异常，通知上层证书申请失败

#### Scenario: DNS 清理本身失败
- **WHEN** 清理 DNS 记录时阿里云 API 调用失败
- **THEN** 系统记录错误日志
- **AND** 不阻断原始异常的上抛

### Requirement: 多段 TLD 域名解析正确
系统在分割域名时，SHALL 对多段公共后缀域名（如 `.com.cn`、`.co.uk`）正确识别根域和子域名。

#### Scenario: 标准域名分割
- **WHEN** 域名为 `www.example.com`
- **THEN** `rr = "www"`, `rootDomain = "example.com"`

#### Scenario: 多段 TLD 域名分割
- **WHEN** 域名为 `test.example.com.cn`
- **THEN** `rr = "test"`, `rootDomain = "example.com.cn"`
