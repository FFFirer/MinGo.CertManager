## 1. 供应商目录与配置态

- [x] 1.1 新增只读供应商目录模型与注册表（ProviderType / DisplayName / ImplementationStatus / ConfigSectionName / ConfigDocHint / HasClient），阿里云 BuiltIn，Cloudflare 与 DNSPod 为 Planned
- [x] 1.2 实现配置态判定与脱敏工具（已配置 = AccessKey 均非空；AccessKeyId 前缀掩码；Secret 仅「已提供/未提供」）
- [x] 1.3 单元测试：目录内容、配置态判定、脱敏输出（含 Secret 不回显）

## 2. 连接体检

- [x] 2.1 实现连接体检服务：复用 `IDomainProviderClient.ListZonesAsync`，映射成功 / 鉴权失败 / 连接失败三态，返回已购域名数量
- [x] 2.2 未配置时禁止调用供应商 API，返回明确的「请先配置」结果
- [x] 2.3 单元测试：体检三态映射、未配置拦截（mock 客户端边界）

## 3. 控制台页面

- [x] 3.1 新增 `Providers.razor`（`/dns-providers`，`[Authorize]`）：卡片式目录、配置态展示、配置指引入口、体检按钮与结果区
- [x] 3.2 `HeaderNav.razor` 顶级导航加入「DNS 供应商」入口（桌面 + 移动菜单）
- [x] 3.3 规划中供应商展示占位文案且不提供体检操作

## 4. 既有页面引导

- [x] 4.1 `Apply.razor` DNS 提供商区块改为只读说明 + 控制台链接，去掉禁用装饰框观感
- [x] 4.2 `Domains.razor`「从提供商同步」在未配置时给出前置提示与控制台入口，不透传 API 异常

## 5. 验证与收尾

- [ ] 5.1 手测：已配置/未配置两态下目录、脱敏、体检、同步前置提示、申请证书引导
- [x] 5.2 运行测试与构建，确认无规格回归（domain-provider-sync / domain-management 需求不变）
