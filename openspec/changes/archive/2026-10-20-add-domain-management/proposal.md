## Why

MinGo.CertManager 目前把域名当作证书上的一段裸字符串（`Certificate.Domain`），没有域名资产台账，也没有提供商侧的已购域名/解析记录视图。申请证书时「DNS 提供商」下拉框是装饰项，`DnsProvider` 实体半成品闲置，DNS 接口写死 `IAliyunDnsService`，出问题只能翻日志。需要以提供商为源同步域名资产（已购域名 + 域名解析），以 FQDN 为管理单元，并留下可追溯的操作记录。

## What Changes

- 新增域名资产模型：`DomainZone`（已购域名）、`Domain`（FQDN 管理单元）、`DomainRecord`（解析记录镜像）
- 新增提供商抽象 `IDomainProviderClient`（ListZones / ListRecords / CreateTxt / DeleteTxt），阿里云为第一个实现；收编现有 `IAliyunDnsService` 职责
- 凭据沿用 `appsettings.AliyunDns` 配置方案，不入库、不新建密钥管理
- 从阿里云同步已购域名与解析记录，推导/合并 FQDN；支持手动添加 FQDN（必须挂在已同步 Zone 下）
- 提供商侧删除的资产本地软删（`Removed`），不物理删除
- 新增 `OperationLog` 操作流水（谁/何时/对谁/做了什么/结果/TraceId），覆盖同步、域名增删、DNS TXT 增删、证书申请
- 新增「域名管理」页面：Zone/FQDN/解析列表、手动同步、手动添加 FQDN、操作记录查询
- 申请证书改为从已纳管 FQDN 选择域名，并带上提供商上下文

## Capabilities

### New Capabilities
- `domain-management`: 域名资产管理（Zone/FQDN/解析记录），基于提供商同步，FQDN 为管理单元，含手动纳管与状态（Active/Removed）
- `domain-provider-sync`: 提供商抽象与阿里云实现；同步已购域名与域名解析；DNS TXT 操作走统一客户端；凭据来自现有配置
- `operation-log`: 域名与 DNS 相关操作的可追溯流水记录

### Modified Capabilities
<!-- 无 — 现有 oauth-login / external-identity-management / reverse-proxy-support 的需求不变；
     证书申请流程的实现细节会接入域名管理，但不改变证书申请本身的需求定义 -->

## Impact

- **Core**：新增 `DomainZone` / `Domain` / `DomainRecord` / `OperationLog` 实体与枚举；新增 `IDomainProviderClient` 抽象（替代 `IAliyunDnsService` 的扩展点）
- **Infrastructure**：`AliyunDomainProviderClient`、`DomainSyncService`、仓储与 EF 迁移；`AliyunDnsService` 逻辑并入新客户端
- **Application**：域名管理用例、同步用例、OperationLog 写入
- **Web**：`Domains.razor` 域名管理页；`Apply.razor` 改为从纳管 FQDN 选择；导航入口
- **测试**：同步推导、软删策略、OperationLog 写入、域名分割与客户端契约的单元测试
- **兼容性**：凭据配置节不变；现有证书表结构不破坏；`IAliyunDnsService` 调用点迁移至 `IDomainProviderClient`
