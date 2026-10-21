## ADDED Requirements

### Requirement: 创建解析记录（A/AAAA/CNAME）

系统 SHALL 支持在已同步的 Zone 下创建 A、AAAA、CNAME 解析记录，并以提供商为真相源。

- 创建 SHALL 先调用提供商 API，成功后回写本地镜像
- 提供商失败时 SHALL NOT 变更本地资产，且 SHALL 写入失败操作记录
- 支持类型 SHALL 仅限 A、AAAA、CNAME
- TTL SHALL 可配置，默认 600
- A/AAAA 的值 SHALL 校验为合法 IP；CNAME 的值 SHALL 校验为合法主机名
- CNAME 的 RR SHALL NOT 为 `@` 或空
- 仅 Admin SHALL 可创建解析记录
- 每次成功创建 SHALL 写入操作记录

#### Scenario: 成功创建 A 记录

- **WHEN** Admin 在 Zone `example.com` 创建 RR=`www`、类型=A、值=`1.2.3.4`、TTL=600
- **THEN** 系统 SHALL 在提供商创建记录，本地 SHALL 存在对应 DomainRecord 镜像，FQDN 台账 SHALL 含 `www.example.com`
- **AND** 系统 SHALL 写入成功操作记录

#### Scenario: 提供商失败不污染本地

- **WHEN** 提供商 API 创建失败
- **THEN** 本地 SHALL NOT 新增 DomainRecord，系统 SHALL 写入失败操作记录

#### Scenario: 非法类型拒绝

- **WHEN** 用户提交 MX 或其它非 A/AAAA/CNAME 类型
- **THEN** 系统 SHALL 拒绝且不调用提供商创建接口

#### Scenario: 根 CNAME 拒绝

- **WHEN** 用户对 RR=`@` 创建 CNAME
- **THEN** 系统 SHALL 拒绝并给出明确提示

### Requirement: 删除解析记录

系统 SHALL 支持删除已存在的解析记录，删除 SHALL 调用提供商 API。

- 删除 SHALL 以提供商记录标识定位并调用提供商删除
- 提供商删除成功后，本地记录 SHALL 置为 Removed（软删保留）
- 提供商失败时 SHALL NOT 变更本地状态
- SHALL 拒绝删除 RR 前缀为 `_acme-challenge.` 的记录
- 仅 Admin SHALL 可删除解析记录
- 每次成功删除 SHALL 写入操作记录
- 本地 Removed 行的清理 SHALL 由同步负责，系统 SHALL NOT 提供独立的本地清理入口

#### Scenario: 成功删除

- **WHEN** Admin 删除 Zone `example.com` 下 RR=`www` 的 A 记录
- **THEN** 系统 SHALL 调用提供商删除该记录，本地记录 SHALL 为 Removed
- **AND** 系统 SHALL 写入成功操作记录

#### Scenario: 保护 ACME 记录

- **WHEN** 用户尝试删除 RR=`_acme-challenge.www` 的记录
- **THEN** 系统 SHALL 拒绝删除且不调用提供商删除接口

#### Scenario: 删除后 FQDN 对齐

- **WHEN** 删除后某 Synced FQDN 已无任何 Active 解析记录
- **THEN** 对应 Domain 资产 SHALL 为 Removed；若仍存在其它 Active 记录则 SHALL 保持 Active

### Requirement: 仅创建与删除，不提供修改

系统 SHALL NOT 提供解析记录的修改（Update）能力。调整记录内容 SHALL 通过删除后重新创建完成。

#### Scenario: 无修改入口

- **WHEN** 用户查看解析记录列表
- **THEN** 系统 SHALL 仅提供创建与删除操作，SHALL NOT 提供修改记录的编辑入口

### Requirement: 提供商客户端通用记录写契约

`IDomainProviderClient` SHALL 扩展通用解析记录创建与删除能力，且 SHALL 保留既有 ACME TXT 专用契约不变。

- SHALL 提供按 Zone/RR/类型/值/TTL 创建记录并返回提供商记录标识
- SHALL 提供按提供商记录标识删除记录
- SHALL NOT 在本次引入记录修改（Update）契约
- 阿里云实现 SHALL 支持 A、AAAA、CNAME
- 阿里云实现 SHALL 以 partial 分文件组织（与 Zone / ACME TXT 能力分离）

#### Scenario: 契约并存

- **WHEN** 业务层调用通用创建记录
- **THEN** 阿里云客户端 SHALL 写入 A/AAAA/CNAME；既有 CreateTxt 等 ACME 方法 SHALL 行为不变

### Requirement: 操作流水覆盖记录创建与删除

操作流水 SHALL 记录解析记录的创建与删除（成功与失败）。

#### Scenario: 记录创建流水

- **WHEN** 解析记录创建成功或失败
- **THEN** 系统 SHALL 写入含动作、目标、结果与摘要的操作记录

#### Scenario: 记录删除流水

- **WHEN** 解析记录删除成功或失败
- **THEN** 系统 SHALL 写入含动作、目标、结果与摘要的操作记录
