## ADDED Requirements

### Requirement: 域名资产三层模型

系统 SHALL 以提供商为源维护域名资产，包含已购域名（Zone）、FQDN 管理单元与解析记录（Record）。

- `DomainZone` SHALL 表示提供商侧已购域名（如 `example.com`），含 ProviderType、域名、状态、同步时间
- `Domain` SHALL 表示 FQDN 管理单元（如 `www.example.com`、`*.example.com`），含所属 Zone、来源（Synced/Manual）、状态（Active/Removed）
- `DomainRecord` SHALL 镜像提供商解析记录，含 Zone、RR、类型、值、TTL、提供商记录标识
- 管理单元 SHALL 为 FQDN，与证书申请域名粒度一致

#### Scenario: 资产可区分来源与状态
- **WHEN** 域名由同步产生
- **THEN** 其 `Source` SHALL 为 `Synced`
- **WHEN** 域名由用户手动添加
- **THEN** 其 `Source` SHALL 为 `Manual`
- **WHEN** 域名在提供商侧已不存在
- **THEN** 其 `Status` SHALL 变为 `Removed` 且记录 SHALL 保留

### Requirement: FQDN 推导

系统 SHALL 根据解析记录的 RR 与 Zone 名推导 FQDN 并合并进域名资产。

- `RR=@` SHALL 推导为 Zone 本身（如 `example.com`）
- 普通 RR SHALL 推导为 `{rr}.{zone}`（如 `www` + `example.com` → `www.example.com`）
- 含 `*` 的 RR SHALL 保留为泛域名 FQDN（如 `*.api.example.com`）
- Zone 本身 SHALL 在无记录时也作为一条 FQDN 资产存在
- 同一 FQDN SHALL 幂等合并，不重复创建

#### Scenario: 标准记录推导
- **WHEN** Zone `example.com` 下存在 RR=`www` 的 A 记录
- **THEN** 系统 SHALL 存在 FQDN 资产 `www.example.com`

#### Scenario: 根记录推导
- **WHEN** Zone `example.com` 下存在 RR=`@` 的记录
- **THEN** 系统 SHALL 存在 FQDN 资产 `example.com`

#### Scenario: 泛域名记录推导
- **WHEN** Zone `example.com` 下存在 RR=`*` 的记录
- **THEN** 系统 SHALL 存在 FQDN 资产 `*.example.com`

### Requirement: 手动添加 FQDN

系统 SHALL 支持手动添加 FQDN 域名资产。

- 手动添加的 FQDN SHALL 归属到已同步的 Zone（按最长后缀匹配）
- 无法匹配任何已同步 Zone 时 SHALL 拒绝添加并给出明确错误
- 手动添加 SHALL 写入操作记录

#### Scenario: 成功添加
- **WHEN** 已同步 Zone `example.com`，用户添加 `dev.example.com`
- **THEN** 系统 SHALL 创建 `Source=Manual`、`Status=Active` 的 FQDN 资产

#### Scenario: 无匹配 Zone 拒绝
- **WHEN** 仅同步了 Zone `example.com`，用户添加 `foo.other.org`
- **THEN** 系统 SHALL 拒绝并提示不存在匹配的已购域名

### Requirement: 域名资产查询

系统 SHALL 提供域名资产列表查询，支持按 FQDN、状态、来源过滤，并可查看 Zone 下解析记录。

#### Scenario: 按状态过滤
- **WHEN** 用户在域名列表选择「仅显示已移除」
- **THEN** 系统 SHALL 仅展示 `Status=Removed` 的 FQDN

#### Scenario: 查看解析记录
- **WHEN** 用户打开某 Zone 的解析记录
- **THEN** 系统 SHALL 展示该 Zone 下镜像的 DomainRecord 列表
