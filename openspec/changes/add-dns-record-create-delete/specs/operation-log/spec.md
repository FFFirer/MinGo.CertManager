## ADDED Requirements

### Requirement: 解析记录创建与删除流水

系统 SHALL 在既有操作流水动作基础上，覆盖解析记录的手动创建与删除。

- 动作 SHALL 包含解析记录创建、解析记录删除（不含修改）
- 成功与失败 SHALL 均写入操作记录
- 目标类型 SHALL 为 DomainRecord，并记录提供商标识或本地标识

#### Scenario: 手动创建记录

- **WHEN** Admin 创建 A/AAAA/CNAME 解析记录
- **THEN** 系统 SHALL 写入对应创建动作的操作记录（成功或失败）

#### Scenario: 手动删除记录

- **WHEN** Admin 删除解析记录
- **THEN** 系统 SHALL 写入对应删除动作的操作记录（成功或失败）
