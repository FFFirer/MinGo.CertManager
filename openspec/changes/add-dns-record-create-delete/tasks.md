## 1. 契约与校验

- [x] 1.1 `IDomainProviderClient` 新增 `CreateRecordAsync` / `DeleteRecordAsync`（不做 Update）；`OperationLogAction` 增加 `RecordCreated` / `RecordRemoved`
- [x] 1.2 Core 增加 A/AAAA/CNAME 值与 TTL 校验纯函数（CNAME 禁根 RR；TTL 默认 600）
- [x] 1.3 单元测试：合法/非法 IP、主机名、根 CNAME、TTL 默认与边界

## 2. 阿里云 partial 实现

- [x] 2.1 将 `AliyunDomainProviderClient` 拆为 partial：`Zones` / `Records` / `AcmeTxt`（AcmeTxt 保持原逻辑）
- [x] 2.2 在 `Records` partial 实现通用创建（AddDomainRecord）与删除（DeleteDomainRecord），支持 A/AAAA/CNAME
- [x] 2.3 单元测试：类型映射与删除定位（mock 边界）；现有 ACME 测试不回归

## 3. 直通写服务与 FQDN 对齐

- [x] 3.1 新增解析记录管理服务：创建/删除按「Provider API → 本地 Upsert/软删」顺序，失败不改本地
- [x] 3.2 从同步逻辑复用/抽取 Zone 级记录+FQDN 对齐（创建恢复 FQDN、删除后无记录且非 Manual 则软删）
- [x] 3.3 创建/删除写入操作流水；拒绝删除 `_acme-challenge.` 前缀记录
- [x] 3.4 单元测试：成功回写、失败不落库、ACME 前缀保护、FQDN 对齐（含 Manual 保留）

## 4. 界面

- [x] 4.1 `Domains.razor` 解析 Tab：添加（Zone 锁定、RR/类型/值/TTL 默认 600）与删除（二次确认）
- [x] 4.2 Admin 权限控制写操作按钮；`_acme-challenge.` 行不显示删除并标注占用；无编辑入口
- [x] 4.3 提供商失败展示错误摘要，不改动本地列表状态

## 5. 验证

- [ ] 5.1 手测：Admin 创建/删除 A、AAAA、CNAME；非 Admin 无写入口；ACME 记录不可删
- [x] 5.2 运行测试与构建；确认 ACME/申请证书流程无改动回归
