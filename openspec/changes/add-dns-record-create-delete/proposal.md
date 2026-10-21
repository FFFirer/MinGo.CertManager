## Why

域名解析目前只是同步镜像 + ACME 专用 TXT 增删，用户无法在界面添加/删除 A、AAAA、CNAME 记录，排障和临时改解析只能去云厂商控制台。需要在证书管理器内提供最小可用的解析记录直通写能力（创建/删除），并保持本地资产与提供商一致。

## What Changes

- `IDomainProviderClient` 新增通用记录 **创建 / 删除** 方法（不改现有 ACME TXT 契约）；**不做 Update**
- 阿里云实现按 partial 分文件组织，支持 A / AAAA / CNAME，**仅 TTL 字段，默认 600**
- 写模型为 **直通写**：先调供应商 API，成功后回写本地 `DomainRecord` 镜像；失败则本地不变更
- 删除 **调用供应商 API**，成功后本地置 `Removed`（软删保留）；本地清理由同步负责，不提供「清理本地」
- `/domains` 解析 Tab 增加添加/删除操作，**Admin 权限**；`_acme-challenge.*` 记录禁止删除
- 每次创建/删除写入操作流水（新 Action）
- FQDN 台账：实体结构不变；创建/删除后更新推导出的 `Domain` 资产（Zone 级对账复用）
- **不做**：Update、MX/SRV/CAA、TXT 界面管理、ACME/申请证书流程改动、`Domain` 实体结构变更、草稿/队列

## Capabilities

### New Capabilities

- `dns-record-write`: 解析记录直通写（创建/删除 A/AAAA/CNAME），供应商为真相源，本地镜像回写，Admin 与 ACME 记录保护

### Modified Capabilities

- `domain-provider-sync`: 提供商客户端契约新增通用记录创建/删除能力（不含 Update）；与既有同步/TXT 能力并存
- `operation-log`: 操作动作覆盖解析记录创建/删除
- `domain-management`: 记录创建/删除后 FQDN 资产对齐规则（行为补充，实体模型不变）

## Impact

- **Core**：`IDomainProviderClient` +2 方法；`OperationLogAction` +2 枚举；可选 A/AAAA/CNAME 校验纯函数
- **Infrastructure**：`AliyunDomainProviderClient` 拆 partial（Zones / Records / AcmeTxt），实现 `AddDomainRecord` / `DeleteDomainRecord`
- **Application**：新增记录直通写服务；`DomainSyncService` 抽出 Zone 级 FQDN 对账供复用（行为级，不改同步语义）
- **Web**：`Domains.razor` 解析列表操作列与添加表单；Admin 控制；`Apply` / ACME **零改动**
- **数据**：无 EF 迁移（`DomainRecord` / `Domain` 结构不变）
- **测试**：类型/TTL 校验、直通写回写与失败回滚语义、`_acme-challenge` 保护、FQDN 对账
