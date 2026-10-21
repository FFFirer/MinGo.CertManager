## ADDED Requirements

### Requirement: 续期操作流水

系统 SHALL 在证书续期成功与失败时写入操作流水，覆盖自动续期与手动续期。

- 成功动作 SHALL 为 `CertRenewed`
- 失败动作 SHALL 为 `CertRenewFailed`
- 流水 SHALL 记录源证书标识与新证书标识（或失败信息）
- 目标类型 SHALL 可识别为 Certificate

#### Scenario: 续期成功写流水

- **WHEN** 证书续期成功（新建记录变为 `Active`）
- **THEN** 系统 SHALL 写入 `CertRenewed` 操作记录

#### Scenario: 续期失败写流水

- **WHEN** 证书续期失败（新建记录变为 `Failed`）
- **THEN** 系统 SHALL 写入 `CertRenewFailed` 操作记录，并包含失败原因
