# domain-provider-sync Specification

## Purpose
TBD - created by archiving change add-domain-management. Update Purpose after archive.
## Requirements
### Requirement: 提供商客户端抽象

系统 SHALL 提供 `IDomainProviderClient` 作为 DNS 提供商能力的统一扩展点。

- SHALL 支持列出已购域名（Zones）
- SHALL 支持列出指定 Zone 的解析记录
- SHALL 支持创建/删除 ACME DNS-01 所需 TXT 记录
- SHALL 标识所属 `DnsProviderType`
- 凭据 SHALL 由各实现从配置读取，不出现在接口契约中
- 阿里云 SHALL 为第一个实现；扩展新提供商 SHALL 不修改业务层调用点

#### Scenario: 阿里云实现满足契约
- **WHEN** 使用阿里云客户端列出 Zones 与 Records
- **THEN** SHALL 返回与阿里云 DNS API 一致的域名与记录数据

#### Scenario: 凭据来自现有配置
- **WHEN** 应用启动
- **THEN** 阿里云客户端 SHALL 使用 `AliyunDns` 配置节的 AccessKey，而非数据库

### Requirement: 从阿里云同步域名资产

系统 SHALL 支持从阿里云同步已购域名与域名解析到本地资产。

- 同步 SHALL 先拉取已购域名列表，再逐域名拉取解析记录
- 同步 SHALL 全量 upsert Zone 与 Record（按提供商记录标识幂等）
- 同步 SHALL 推导并合并 FQDN 资产
- 提供商侧已删除的 Zone/Record/FQDN SHALL 本地软删
- 每次同步 SHALL 写入操作记录，含成功/失败与变更计数摘要
- 同步 SHALL 支持界面手动触发

#### Scenario: 首次同步
- **WHEN** 用户触发同步且阿里云账号下有 `example.com` 及解析记录
- **THEN** 系统 SHALL 创建对应 DomainZone、DomainRecord 与推导出的 Domain

#### Scenario: 提供商删除后同步
- **WHEN** 上次同步存在 `old.example.com`，本次提供商已无该记录
- **THEN** 对应 Domain SHALL 被置为 `Removed` 而非物理删除

#### Scenario: 同步失败
- **WHEN** 调用阿里云 API 失败
- **THEN** 系统 SHALL 记录失败操作记录并保留既有资产不变

### Requirement: DNS-01 TXT 操作走提供商客户端

ACME 流程中 DNS-01 TXT 记录的创建与清理 SHALL 通过 `IDomainProviderClient` 完成。

- 创建 TXT 前 SHALL 先清理同名残留记录（沿用现有事前清理策略）
- 验证完成或失败后 SHALL 清理 TXT 记录
- 每次创建/删除 TXT SHALL 写入操作记录
- 根域名 SHALL 优先使用 FQDN 所属 Zone，而非仅依赖启发式切分

#### Scenario: 申请证书创建 TXT
- **WHEN** 为 `www.example.com` 发起 DNS-01 验证
- **THEN** 系统 SHALL 在 Zone `example.com` 创建 `_acme-challenge.www` TXT 并写操作记录

#### Scenario: 验证后清理
- **WHEN** DNS-01 验证结束（无论成败）
- **THEN** 系统 SHALL 清理对应 TXT 并写操作记录

