## Context

MinGo.CertManager 是 .NET 10 洋葱架构（Core / Application / Infrastructure / Web）的证书管理器，通过 ACME DNS-01 申请 Let's Encrypt 证书。当前域名只是 `Certificate.Domain` 裸字符串；DNS 能力写死在 `IAliyunDnsService` / `AliyunDnsService`（仅 TXT 增删）；`DnsProvider` 实体半成品闲置，真正凭据在 `appsettings.AliyunDns`；`Apply.razor` 的「DNS 提供商」下拉框是装饰。问题排查只能翻 Serilog / OTel。

需求边界（已对齐）：
- 管理单元 = **FQDN**
- 从提供商同步 **已购域名（Zone）** 与 **域名解析（Records）**
- **操作记录** 可追溯
- 凭据 **沿用 appsettings 方案**，不入库

## Goals / Non-Goals

**Goals:**
- 域名资产（Zone / FQDN / 解析记录）可从阿里云同步并在界面管理
- FQDN 作为申请证书的选择入口，挂靠 Zone
- 所有域名/DNS 相关操作写 `OperationLog`，可按目标/动作/时间查询
- 提供商能力抽象为 `IDomainProviderClient`，阿里云为第一个实现
- 凭据继续读 `AliyunDns` 配置节，部署方式不变
- 失败/删除可追溯：本地软删，不物理抹掉历史

**Non-Goals:**
- 不做多账号/多凭据 profile（凭据仍单一配置）
- 不做 Cloudflare / DNSPod 实现（只留扩展点）
- 不做完整的 DNS 记录在线编辑（同步镜像 + ACME TXT 生命周期即可）
- 不改 ACME 证书签发/续签语义
- 不引入审计合规级防篡改/归档导出

## Decisions

### 决策 1：三层资产模型 — Zone / Domain(FQDN) / Record

| 实体 | 粒度 | 来源 | 用途 |
|---|---|---|---|
| `DomainZone` | 已购域名 `example.com` | `DescribeDomains` | 归属层、根域 |
| `Domain` | FQDN `www.example.com` | 记录推导 + 手动 | **管理单元**，申请证书入口 |
| `DomainRecord` | 解析记录 | `DescribeDomainRecords` | 资产明细、排查 |

**为什么不用单一 Domain 表：** 阿里云 API 按 Zone 操作，证书按 FQDN 申请；解析记录是第三种粒度。压成一张表会丢掉「已购域名」边界和记录明细。

**替代方案：** 只存 FQDN、同步时内联 JSON 记录 → 查询/过滤解析记录困难，放弃。

### 决策 2：FQDN 推导规则

```
Record.RR + Zone.DomainName → FQDN
  "@"     → example.com
  "www"   → www.example.com
  "*.api" → *.api.example.com
```

- Zone 本身即使无记录也 upsert 一条 `Domain`（`example.com`），`Source=Synced`
- 手动添加 `Source=Manual`，**必须**能匹配到已同步 Zone（按最长后缀匹配根域）
- 同一 FQDN 幂等合并：已存在则更新状态/来源标记，不重复建行

### 决策 3：同步语义与软删

1. `ListZones` → upsert `DomainZone`
2. 逐 Zone `ListRecords` → upsert `DomainRecord`（键：Provider + Zone + RecordId）
3. 由记录推导 FQDN → upsert `Domain`
4. 提供商侧已不存在的 Zone/Record/FQDN → **软删** `Status=Removed`（`IsRemoved`/`RemovedAt`），保留行
5. 每次同步写 1 条 `OperationLog`（Action=DomainSynced），Message 含新增/更新/移除计数

**为什么软删：** 「保留可追溯记录」是硬需求；硬删会让操作流水与资产对不上。

### 决策 4：提供商抽象 `IDomainProviderClient`

```csharp
public interface IDomainProviderClient
{
    DnsProviderType ProviderType { get; }
    Task<IReadOnlyList<ProviderZone>> ListZonesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProviderRecord>> ListRecordsAsync(string zoneName, CancellationToken ct = default);
    Task<string> CreateTxtRecordAsync(string zoneName, string rr, string value, CancellationToken ct = default);
    Task DeleteTxtRecordAsync(string zoneName, string rr, string value, CancellationToken ct = default);
}
```

- 现有 `IAliyunDnsService` 的 Clear/Create/Delete **收编**进 `AliyunDomainProviderClient`
- ACME 侧 `AcmeService` 依赖 `IDomainProviderClient`（或按 ProviderType 解析）
- 凭据仍由 `IOptions<AliyunDnsSettings>` 注入实现类，**不进接口**

**为什么不用 DI 多实现立即切换：** 当前只有一个提供商；接口按 `ProviderType` 标识，后续注册 `IEnumerable<IDomainProviderClient>` 即可。

### 决策 5：OperationLog 最小字段

`Id, At, Actor, Action, TargetType, TargetId, TargetName, ProviderType, Result, Message, TraceId, Payload`

- Action 枚举：`ZoneSynced / RecordSynced / DomainSynced / DomainAdded / DomainRemoved / DnsTxtCreated / DnsTxtDeleted / CertRequested`
- Actor：`system`（Job/同步）或当前用户名
- **不存** AccessKey / TXT 明文可截断到安全长度
- TraceId 来自 `Activity.Current?.TraceId`，串 OTel
- ACME TXT 增删也记（排查 challenge 最有价值），界面可按 Action 过滤

### 决策 6：UI 与证书流程接入

- 新页面 `/domains`：Zone 列表、FQDN 列表（含状态/来源）、解析记录查看、手动同步、手动添加 FQDN、操作记录 Tab
- `Apply.razor`：域名输入改为下拉选择 **Active 状态的纳管 FQDN**（支持泛域名勾选），展示所属 Zone
- 仍允许高级场景？→ **否**，强制从纳管 FQDN 选，避免根域与提供商不一致

### 决策 7：DnsProvider 实体

本次 **不启用** 其凭据字段，保持闲置。ProviderType 用独立枚举常量/实体字段即可。后续多账号再开 change。

## Risks / Trade-offs

- [全量镜像解析记录导致表膨胀] → `DomainRecord` 按 (ProviderType, Zone, RecordId) 索引；同步仅 upsert；暂不做分页归档，量级（单账号数百 Zone × 数百记录）对 SQLite 可接受
- [同步与 ACME TXT 操作竞态] → TXT 创建/删除实时写 OperationLog；同步不删除非 `_acme-challenge` 之外的意外匹配；Clear 仍精确匹配 RR
- [`SplitDomainName` 启发式与真实 Zone 不一致] → 手动 FQDN 强制挂已同步 Zone；ACME 根域优先取 Domain 所属 `DomainZone.DomainName`
- [软删数据堆积] → 接受；查询默认 `Status=Active`，可筛 Removed
- [替换 `IAliyunDnsService` 破坏测试] → 保留适配或一次性改测试 mock 为 `IDomainProviderClient`

## Migration Plan

1. 新增实体 + `ApplicationDbContext` 配置 + EF 迁移
2. 实现 `IDomainProviderClient` / `AliyunDomainProviderClient`，替换 `IAliyunDnsService` 注册
3. 实现同步服务与 OperationLog 写入
4. Web 域名页 + Apply 接入
5. 单元测试；启动迁移已由 `MigrationExtensions` 自动应用，回滚 = 迁移 Down / 数据库备份

## Open Questions

- 无阻塞项。若后续要多提供商凭据或记录在线编辑，单开 change。
