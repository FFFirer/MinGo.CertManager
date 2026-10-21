## ADDED Requirements

### Requirement: 通用解析记录创建与删除契约

系统 SHALL 在提供商客户端抽象上提供通用解析记录创建与删除能力，供域名解析管理使用。

- SHALL 支持按 Zone、RR、记录类型、值、TTL 创建解析记录，返回提供商记录标识
- SHALL 支持按提供商记录标识与 Zone 删除解析记录
- 支持类型 SHALL 为 A、AAAA、CNAME（由调用方约束）
- SHALL NOT 本次引入解析记录修改（Update）契约
- 既有 ACME DNS-01 TXT 创建/删除/清理契约 SHALL 保持不变
- 阿里云 SHALL 为第一个实现；实现 SHALL 可按 partial 分文件组织

#### Scenario: 创建后可被同步识别

- **WHEN** 通过通用契约创建 A 记录并触发同步
- **THEN** 同步 SHALL 按提供商记录标识幂等 upsert 该记录镜像

#### Scenario: TXT 契约不受影响

- **WHEN** ACME 流程调用 TXT 创建/删除
- **THEN** 行为 SHALL 与扩展通用契约前一致
