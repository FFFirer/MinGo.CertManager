## Context

MinGo.CertManager 已完成域名资产三层模型（Zone / FQDN / Record）与 `IDomainProviderClient` 提供商抽象，阿里云是唯一实现，凭据来自 `AliyunDns` 配置节（env / user-secrets / appsettings），**明确不入库**。`DnsProvider` 实体、`DnsProviderRepository`、`DnsProviders` 表全链路闲置（design 决策 7：凭据字段不启用）。

当前缺口：供应商本身不可见——

- 申请证书页 `Apply.razor` 的「DNS 提供商」是禁用文本框，看不出配置是否生效
- 域名管理「从提供商同步」在密钥为空时直接 API 失败，无前置提示
- `DnsProviderType` 中 Cloudflare / DNSPod 占位无处展示
- 没有连接体检入口，排障只能翻 Serilog / OTel

需求边界（已对齐）：走**路线 A**——只读供应商控制台。目录可视化 + 配置态 + 连接体检 + 配置指引。**不管凭据**。

## Goals / Non-Goals

**Goals:**

- 内置 DNS 供应商目录可视化（阿里云 = 内置；Cloudflare / DNSPod = 规划中占位）
- 配置态可感知：是否已配置、AccessKeyId 脱敏、Region / Endpoint；Secret 永不回显
- 连接体检（只读 `ListZones`），三态反馈（成功 / 鉴权失败 / 网络错误）
- 配置指引入口（user-secrets / 环境变量写法）
- 申请证书页、域名管理页的 DNS 供应商相关引导修正
- 零数据库迁移；`IDomainProviderClient` 契约与「凭据不入库」规格不动

**Non-Goals:**

- 不做凭据录入 / 修改 / 回显 Secret
- 不做密钥入库、加密存储、多账号 profile
- 不做 Cloudflare / DNSPod 实现（只留目录占位）
- 不做 TXT 写权限探测（有副作用）
- 不改 `IDomainProviderClient` 接口、不改资产实体键（`ProviderType` 不动）
- 不启用 `DnsProvider` 实体 / `DnsProviderRepository`

## Decisions

### 决策 1：目录放代码注册表，不放数据库

新增只读的 `IDnsProviderCatalog`（或静态注册表）描述内置供应商：

| 字段 | 用途 |
|---|---|
| `ProviderType` | 对齐 `DnsProviderType` 枚举 |
| `DisplayName` | 列表展示（如「阿里云 DNS」） |
| `ImplementationStatus` | `BuiltIn` / `Planned` |
| `ConfigSectionName` | 「凭据来源」提示（如 `AliyunDns`） |
| `ConfigDocHint` | 配置写法指引文案 |
| `HasClient` | 是否可体检（Planned 为 false） |

**为什么不用 DB：** 仅内置实现、只读展示；入库等于给闲置的 `DnsProvider` 表找活，和决策 7 冲突。**替代方案：** 用枚举反射硬编码在页面里 → 目录字段膨胀后难测、难扩，放弃。

### 决策 2：配置态判定 = 非空，不回显 Secret

```
已配置 = AccessKeyId 非空 && AccessKeySecret 非空
AccessKeyId 展示：前 4–6 位 + ****
Secret：只显示「已提供 / 未提供」，永不回显
```

**为什么：** 控制台是只读的，无需展示密钥；脱敏是对齐 `OperationLog`「禁止包含 AccessKey/Secret」的既有安全习惯。

### 决策 3：体检只做只读 `ListZones`

| 探测 | 结论 |
|---|---|
| `ListZonesAsync`（DescribeDomains） | ✅ 验证密钥有效 + 网络连通 |
| `ListRecordsAsync` | 与 ListZones 同鉴权，信息增量小，不做 |
| `CreateTxt` / `DeleteTxt` | 有副作用，不做 |

体检结果三态映射到 UI：

- 成功：「连接正常 · 检出 N 个已购域名」
- 鉴权失败（HTTP 4xx / InvalidAccessKeyId 等）：「鉴权失败 · 请检查 AccessKeyId/Secret」
- 网络/超时/其它：「连接失败 · …」

### 决策 4：体检结果不写 OperationLog

操作流水（`operation-log`）记录**改变资产的动作**（同步、增删、TXT、申请证书）。体检是只读探测，高频点击会刷屏。结果仅页面展示（本次响应 + 可选内存缓存「最近体检时间/结果」）。

**替代方案：** 新增 `ProviderTested` Action → 扩枚举 + 噪音，收益低，放弃。

### 决策 5：权限 `[Authorize]`，入口在顶级导航

只读目录 + 体检，不含密钥编辑，登录即可。顶级导航「DNS 供应商」与「域名管理」并列——它是域名资产的上游，一等公民；日后若走路线 B 入口不用搬家。

**替代方案：** 放用户菜单 Admin-only（与 API Key 同级）→ 权限过重，且降低可见性。

### 决策 6：申请证书页禁用框 → 引导

`Apply.razor` 的「DNS 提供商」区块改为静态展示「当前提供商：阿里云 DNS · 凭据由配置提供」+ 链接 `/dns-providers`。去掉装饰性 InputSelect 观感。

### 决策 7：同步前置提示

`Domains.razor`「从提供商同步」在配置态为「未配置」时，点击给出明确错误/禁用态：「请先在 DNS 供应商页查看配置说明并完成 AliyunDns 凭据配置」，避免直接抛 API 异常。

## Risks / Trade-offs

- [只读控制台被误解为可改密钥] → 页面标题/描述明确写「凭据来自部署配置，不在界面修改」；配置说明卡片写清 user-secrets / env 写法
- [体检刷第三方 API / 被限流] → 仅用户主动点击触发，不自动轮询；失败文案提示稍后重试
- [Planned 占位让用户以为即将上线] → 文案用「规划中 / 暂未内置实现」，不承诺时间
- [配置态判定过粗（只有非空）] → 接受；真正的有效性由「测试连接」回答
- [与未来路线 B（凭据入库）冲突] → 目录注册表与页面结构兼容多实例展示；本次不预留 DB 位，真到多账号再开 change

## Migration Plan

无数据库迁移。部署即生效：

1. 新增目录注册表 + 体检服务 + Providers 页面 + 导航
2. 修正 Apply / Domains 引导与前置提示
3. 单元测试（目录、脱敏、体检三态）
4. 回滚 = 移除页面与导航入口，无数据残留

## Open Questions

- 无阻塞项。体检缓存时长（若做内存缓存）实现时按「点击即测、不持久化」处理即可。
