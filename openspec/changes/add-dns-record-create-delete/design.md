## Context

MinGo.CertManager 已有：

- `DomainZone` / `DomainRecord`（镜像）/ `Domain`（FQDN 管理单元）三层资产，来源为提供商同步
- `IDomainProviderClient`：ListZones / ListRecords + ACME 专用 TXT 增删清理
- `Domains.razor` 解析 Tab **只读**；`DomainRecord` 带 `ProviderRecordId`，可回写
- 操作流水 `OperationLog`；FQDN 由 `RR+Zone` 推导并在同步时对账

上一轮 `add-domain-management` 明确 Non-Goal：「不做完整的 DNS 记录在线编辑」。现按已拍板边界补最小写能力。

**已锁定约束：**

| 项 | 决定 |
|---|---|
| 写模型 | 直通写（Provider 为真相源） |
| 操作 | **仅创建 + 删除**，不做 Update |
| 类型 | 仅 A / AAAA / CNAME |
| 字段 | 仅 TTL，**默认 600** |
| 删除 | 调供应商 API；本地软删；**本地清理由同步负责** |
| 权限 | Admin |
| 接口 | 扩 `IDomainProviderClient` 通用方法；阿里云 partial 分文件 |
| ACME | **本 change 零改动** |
| FQDN | 结构不改；行为纳入（增删后对账） |

## Goals / Non-Goals

**Goals:**

- 界面创建/删除 A、AAAA、CNAME，写穿到阿里云后回写本地镜像
- 失败不改本地；成功路径写操作流水
- 删除后本地 `Removed`；FQDN 台账与记录一致（复用推导）
- `_acme-challenge.*` 保护，避免破坏 DNS-01
- 零 EF 迁移、零 ACME 改动

**Non-Goals:**

- Update / 改类型 / 改 TTL（要改则删了重建）
- MX、NS、SRV、CAA、TXT 界面管理
- Priority、Line（线路）、批量导入/导出
- 草稿、离线队列、冲突合并 UI
- `Domain` / `DomainRecord` 表结构变更
- 申请证书流程、`AcmeService` 改动
- 「清理本地 Removed 行」的独立入口（同步负责）

## Decisions

### 决策 1：直通写，失败不落本地

顺序固定为 **Provider API → 本地 Upsert/软删**。

- 本地没有 `ProviderRecordId` 就无法定位删除；创建成功才拿到回写 id
- 失败时本地保持原状，流水 `Result=Failed`
- **替代方案：** 本地先写再推送 → 引入未发布态与回滚，过重，放弃

### 决策 2：接口只加 Create/Delete，不加 Update

```csharp
Task<string> CreateRecordAsync(
    string zoneName, string rr, string recordType, string value, int ttl,
    CancellationToken ct = default);

Task DeleteRecordAsync(
    string providerRecordId, string zoneName,
    CancellationToken ct = default);
```

- 复用现有 `ListRecordsAsync` / `UpsertRecordAsync` 做对账
- ACME 的 `CreateTxtRecordAsync` 等 **保持不动**
- **不做 Update** 的原因：改 TTL/值/类型语义纠缠（CNAME 冲突、类型变更=删+增）；最小闭环「删了重建」已够用

### 决策 3：类型与 TTL 约束在应用层校验

| 类型 | Value | TTL |
|---|---|---|
| A | IPv4 | 默认 **600**；允许范围按阿里云（实现时以 API 报错兜底，UI 建议 600–86400） |
| AAAA | IPv6 | 同上 |
| CNAME | 合法主机名 | 同上 |

- RR 为空或 `@` 的 CNAME：**拒绝**（根 CNAME 受限）
- RR 以 `_acme-challenge.` 开头：**拒绝删除**（创建也不在 UI 开放）
- 同 RR 已存在其它类型记录：创建前本地提示冲突，仍允许提交时以提供商响应为准

校验放 **Core 纯函数**（便于单测），Application 调用。

### 决策 4：partial 分文件

```
AliyunDomainProviderClient.cs            ctor / NormalizeEndpoint / ProviderType
AliyunDomainProviderClient.Zones.cs      ListZones
AliyunDomainProviderClient.Records.cs    ListRecords + CreateRecord + DeleteRecord
AliyunDomainProviderClient.AcmeTxt.cs    CreateTxt / DeleteTxt / ClearTxt
```

通用方法只操作 A/AAAA/CNAME 所需字段（rr/type/value/ttl），映射 `AddDomainRecord` / `DeleteDomainRecord`。

### 决策 5：删除 = API 删 + 本地软删；清理靠同步

- `DeleteRecordAsync(providerRecordId)` 成功后本地 `Status=Removed`
- 不物理删行（对齐「软删可追溯」）
- 不提供「清理本地」按钮；下次同步全量对账继续维护
- **替代方案：** 删成功即物理删 → 流水目标引用悬空，放弃

### 决策 6：FQDN 行为纳入，结构不动

创建/删除记录后：

1. 按 `RR+Zone` 推导 FQDN（`DomainNameHelper.ToFqdn`）
2. 创建：若 FQDN 不存在则 upsert `Source=Synced`；已 Removed 则恢复 Active（与同步一致）
3. 删除：不立刻删 FQDN 行；若该 FQDN 已无其它 Active 记录 **且** 非 Manual，则置 Removed（与同步软删一致）

为此从 `DomainSyncService` 抽出 **Zone 级记录+FQDN 对账** 最小接口（或复用其私有逻辑），同步与记录写共用。**不改** 同步整体语义与手动 FQDN 规则。

**不纳入：** 合并 `Domain` 进 `DomainRecord`、给 `Domain` 加 RecordId 外键。

### 决策 7：Admin 与 `_acme-challenge` 保护

- 解析增删：`[Authorize(Roles = "Admin")]` 控制按钮；服务层二次校验 Actor
- 读列表保持登录可见（与现 `/domains` 一致）
- `_acme-challenge.` 前缀：服务层 **Delete 直接拒绝**，UI 不显示删除按钮并标注「证书验证占用」
- ACME 流程零改动；仅防误删

### 决策 8：操作流水

`OperationLogAction` 追加：

- `RecordCreated`
- `RecordRemoved`

（无 `RecordUpdated`。）Message 含 zone/rr/type/value 截断摘要；`TargetType=DomainRecord`，`TargetId=ProviderRecordId` 或本地 Id。

## Risks / Trade-offs

- [CNAME/其它类型同 RR 冲突由提供商拒绝] → 本地预检提示；错误原文展示
- [根域 CNAME 不可用] → 创建时拒绝并说明
- [误删生产记录] → Admin + 二次确认 + 删除前展示当前 RR/Type/Value
- [破坏 `_acme-challenge` 导致续签失败] → 前缀黑名单在服务层与 UI 双层
- [写后与全量同步窗口不一致] → 接受；「从提供商同步」为最终事实
- [无 Update 导致改 TTL 需删+建] → 接受；文档/文案注明「修改请删除后重建」
- [partial 拆文件引入回归] → AcmeTxt 逻辑保持原方法体；现有 ACME 测试全绿为准

## Migration Plan

无数据库迁移。部署步骤：

1. Core 契约 + 枚举 + 校验
2. 阿里云 partial 拆分 + Create/Delete 实现
3. Application 直通写服务 + FQDN 对账抽取
4. Web 解析 Tab UI
5. 单测；回滚 = 还原代码，无数据残留（本地软删行可保留）

## Open Questions

- 无阻塞项。TTL 上限以阿里云 API 校验为最终边界，UI 默认 600、不强行封顶到 86400（付费版更高）。
