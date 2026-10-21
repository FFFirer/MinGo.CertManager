## ADDED Requirements

### Requirement: 记录写操作后的 FQDN 对齐

解析记录创建或删除后，系统 SHALL 使 FQDN 域名资产与解析记录一致；`Domain` 实体模型 SHALL 不变。

- 创建记录 SHALL 将推导出的 FQDN 置为 Active（不存在则创建 `Source=Synced`，已 Removed 则恢复）
- 删除记录后，若该 FQDN 无其它 Active 解析记录且非 Manual 源，SHALL 将其置为 Removed
- Manual 源 FQDN SHALL 不因记录删除而自动 Removed
- 对齐规则 SHALL 与同步的 FQDN 推导/软删规则一致，可复用同一实现

#### Scenario: 创建后出现 FQDN

- **WHEN** 在 Zone `example.com` 创建 RR=`dev` 的 A 记录
- **THEN** 系统 SHALL 存在 Active 的 FQDN `dev.example.com`（Source=Synced）

#### Scenario: 删除后无记录 FQDN 软删

- **WHEN** 删除 `dev.example.com` 的唯一解析记录
- **THEN** `dev.example.com` SHALL 为 Removed；`Source=Manual` 的同名资产 SHALL 保持不变

#### Scenario: 仍有其它记录则保留

- **WHEN** 删除 `www.example.com` 的 A 记录，但仍有同 FQDN 的其它 Active 记录
- **THEN** FQDN 资产 SHALL 保持 Active
