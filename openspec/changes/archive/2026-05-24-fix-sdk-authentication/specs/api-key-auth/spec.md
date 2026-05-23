## ADDED Requirements

### Requirement: API Key 认证
对所有 `/api/external/*` 路径的请求，系统 SHALL 通过 `Authorization: ApiKey <key>` 头进行身份认证。

#### Scenario: 携带有效 API Key 的请求
- **WHEN** 请求到达 `/api/external/*` 路径，且 `Authorization` 头格式为 `ApiKey <valid_key>`
- **THEN** 中间件通过认证
- **AND** 请求继续处理

#### Scenario: 缺少 API Key 的请求
- **WHEN** 请求到达 `/api/external/*` 路径，且没有 `Authorization` 头
- **THEN** 中间件返回 HTTP 401
- **AND** 返回 JSON 错误码 `AUTH_MISSING_API_KEY`

#### Scenario: 无效 API Key 的请求
- **WHEN** 请求到达 `/api/external/*` 路径，且 `Authorization` 头的 API Key 值不存在或已禁用
- **THEN** 中间件返回 HTTP 401
- **AND** 返回 JSON 错误码 `AUTH_INVALID_API_KEY`

### Requirement: API Key 仅存储哈希值
系统在创建 API Key 时，SHALL 仅存储哈希值，不持久化明文。

#### Scenario: 创建 API Key
- **WHEN** 管理员通过管理页面创建新的 API Key
- **THEN** 系统生成明文 Key 和 Secret 并在界面上展示一次
- **AND** 数据库中仅存储 SHA-256 哈希值
- **AND** 明文不在任何日志或持久化存储中保留

#### Scenario: 列表页不显示明文
- **WHEN** 管理员访问 API Key 列表页
- **THEN** 列表仅显示掩码（`****{last8}`），不显示完整 Key 或 Secret
