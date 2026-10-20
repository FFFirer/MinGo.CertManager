## 1. Core 模型与抽象

- [x] 1.1 新增枚举与实体：`DomainAssetStatus`、`DomainAssetSource`、`OperationLogAction`、`OperationLogResult`，`DomainZone`、`Domain`、`DomainRecord`、`OperationLog`
- [x] 1.2 新增 `IDomainProviderClient` 与 DTO（`ProviderZone`、`ProviderRecord`），替代 `IAliyunDnsService` 扩展点
- [x] 1.3 `ApplicationDbContext` 注册新实体与索引，生成 EF 迁移

## 2. 提供商实现与 ACME 收编

- [x] 2.1 实现 `AliyunDomainProviderClient`（ListZones / ListRecords / CreateTxt / DeleteTxt），读取现有 `AliyunDnsSettings`
- [x] 2.2 将 `AcmeService` / `CertificateService` 的 DNS 调用迁移到 `IDomainProviderClient`，移除对 `IAliyunDnsService` 的依赖
- [x] 2.3 ACME 根域名优先使用 FQDN 所属 `DomainZone`，回退启发式 `SplitDomainName`

## 3. 同步与操作记录

- [x] 3.1 实现 `IOperationLogService` 写入/查询，含 TraceId、脱敏
- [x] 3.2 实现 `IDomainSyncService`：Zone/Record/FQDN upsert、软删、变更计数
- [x] 3.3 同步服务与 ACME TXT 创建/删除写入 OperationLog
- [x] 3.4 仓储：`DomainRepository`（Zone/Domain/Record）、`OperationLogRepository`

## 4. Web UI 与申请接入

- [x] 4.1 新增 `/domains` 页面：Zone、FQDN 列表（状态/来源筛选）、解析记录查看、手动同步、手动添加 FQDN
- [x] 4.2 操作记录 Tab：时间倒序、按动作/目标名过滤
- [x] 4.3 导航加入「域名管理」入口
- [x] 4.4 `Apply.razor` 改为从 Active 纳管 FQDN 选择，展示所属 Zone；移除装饰用 DNS 下拉

## 5. 测试与验证

- [x] 5.1 单元测试：FQDN 推导（@ / 多段 / 泛域名）、手动添加 Zone 匹配与拒绝
- [x] 5.2 单元测试：同步 upsert/软删语义、OperationLog 写入字段
- [x] 5.3 单元测试：AliyunDomainProviderClient 契约映射（mock 边界）
- [x] 5.4 `dotnet build` 与全部测试通过；更新 README/配置说明

## 6. 收尾

- [x] 6.1 核对 spec 场景覆盖，标记任务完成
