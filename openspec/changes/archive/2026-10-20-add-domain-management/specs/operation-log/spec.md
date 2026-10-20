## ADDED Requirements

### Requirement: 操作流水记录

系统 SHALL 为域名资产与 DNS 操作保留可追溯的操作记录（OperationLog）。

- 每条记录 SHALL 含：时间、操作者、动作、目标类型、目标标识、目标名称、提供商类型、结果、摘要消息、TraceId
- 动作 SHALL 至少覆盖：同步、域名手动添加、域名移除、DNS TXT 创建/删除、证书申请触发
- 结果 SHALL 区分 Success / Failed
- 失败时 Message SHALL 含可读错误摘要，且 SHALL NOT 包含 AccessKey/Secret
- TraceId SHALL 与 OpenTelemetry Trace 关联（存在时写入）

#### Scenario: 记录同步结果
- **WHEN** 用户触发一次域名同步且部分成功
- **THEN** 系统 SHALL 写入一条同步操作记录，含变更计数与结果

#### Scenario: 记录 TXT 操作
- **WHEN** ACME 流程创建 `_acme-challenge` TXT
- **THEN** 系统 SHALL 写入 `DnsTxtCreated` 操作记录，含域名与 Zone
- **WHEN** 该 TXT 被删除
- **THEN** 系统 SHALL 写入 `DnsTxtDeleted` 操作记录

#### Scenario: 记录失败原因
- **WHEN** 阿里云 API 调用失败
- **THEN** 系统 SHALL 写入 `Result=Failed` 的操作记录
- **THEN** Message SHALL 不包含敏感凭据

### Requirement: 操作记录查询

系统 SHALL 提供操作记录查询界面，支持按时间倒序展示并按动作/目标过滤。

- 默认 SHALL 按时间倒序分页展示
- SHALL 支持按动作类型过滤
- SHALL 支持按目标名称（如域名）过滤
- 单条记录 SHALL 展示时间、操作者、动作、目标、结果

#### Scenario: 按动作过滤
- **WHEN** 用户选择仅查看 `DnsTxtCreated`
- **THEN** 列表 SHALL 仅展示该动作的流水

#### Scenario: 按域名过滤
- **WHEN** 用户输入 `www.example.com`
- **THEN** 列表 SHALL 仅展示目标名称匹配的流水
