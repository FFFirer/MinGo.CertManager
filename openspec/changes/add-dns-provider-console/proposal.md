## Why

域名资产已按提供商同步管理，但 DNS 供应商本身没有管理入口：凭据配置是否生效只能翻日志，申请证书页的「DNS 提供商」是禁用装饰框，`DnsProvider` 实体闲置、`Cloudflare`/`Dnspod` 枚举占位不可见。需要一个只读的供应商控制台，让「内置哪些供应商、凭据是否已配、连接是否正常」一眼可见，并为后续扩展留门——同时不触碰「凭据留在配置节」的安全边界。

## What Changes

- 新增「DNS 供应商」控制台页：可视化内置供应商目录（阿里云 DNS 为唯一内置实现，Cloudflare / DNSPod 以「规划中」占位）
- 展示配置态：是否已配置 AccessKey、AccessKeyId 脱敏、Region / Endpoint；Secret 永不回显
- 支持连接体检（只读 `ListZones` 冒烟），展示最近体检结果（成功/鉴权失败/网络错误）
- 提供配置指引入口（user-secrets / 环境变量写法）
- 申请证书页的 DNS 提供商展示改为指向控制台的引导，不再伪装成可选下拉
- 域名管理「从提供商同步」在凭据未配置时给出明确前置提示
- 顶级导航新增入口；权限为登录即可（不含密钥编辑）
- **不做**：凭据录入/修改、密钥入库、多账号实例、Cloudflare/DNSPod 实现、`IDomainProviderClient` 契约变更

## Capabilities

### New Capabilities

- `dns-provider-console`: DNS 供应商目录可视化、配置态感知、连接体检、配置指引；内置实现注册表与「规划中」占位展示

### Modified Capabilities

<!-- 无 — 凭据仍由各实现从配置读取（domain-provider-sync 不变）；域名资产模型与操作流水需求不变 -->
## Impact

- **Web**：新增 `Providers.razor`（或等价页面）；`HeaderNav.razor` 导航入口；`Apply.razor` DNS 提供商区块改为引导；`Domains.razor` 同步前配置态提示
- **Core / Infrastructure**：新增只读的供应商目录（代码内置注册表）与连接体检服务（复用 `IDomainProviderClient.ListZonesAsync`）；`DnsProvider` 实体与 `DnsProviderRepository` 继续闲置不启用
- **配置**：`AliyunDns` 配置节不变；无数据库迁移
- **规格**：`domain-provider-sync` / `domain-management` / `operation-log` 需求不改
- **测试**：目录注册表、配置态判定与脱敏、体检三态映射的单元测试
