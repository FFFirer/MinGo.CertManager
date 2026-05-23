## Context

当前 API Key 认证流程存在三方面问题：

1. **协议不匹配**:
   - SDK 发送: `Authorization: ApiKey <key>` (ApiClient.cs:127)
   - 中间件检查: `X-API-Key` 头 (Middleware.cs:46)
   - SDK 永远无法通过认证

2. **Secret 传输安全**:
   - 中间件要求每次请求带 `X-API-Secret` (Middleware.cs:52)
   - 等效于每次请求明文传输密码
   - 日志系统可能记录请求头

3. **明文存储**:
   - `ApiKey` 实体同时存明文和哈希 (ApiKey.cs:21-36)
   - `GetAllApiKeysAsync` 返回完整明文 (ApiKeyService.cs:116-131)
   - `ApiKeys.razor` 列表页直接展示明文 (ApiKeys.razor:85-94)
   - 设计文档明确要求"只存哈希"

## Goals / Non-Goals

**Goals:**
- SDK 调用 `/api/external/*` 能通过认证
- 停止每次请求明文传输 Secret
- 数据库不再持久化明文 Key/Secret
- 管理页面创建时展示一次明文，列表只显示掩码

**Non-Goals:**
- 不实现 HMAC 签名机制（后续阶段增强）
- 不实现 IP 白名单校验
- 不实现 Nonce 防重放
- 不实现限流

## Decisions

### 决策 1：中间件改为检查 `Authorization: ApiKey <key>` 头

将中间件的认证头从 `X-API-Key` 改为 `Authorization: ApiKey <key>`，与 SDK 发送格式匹配。这是标准 HTTP 认证头格式。

### 决策 2：移除 `X-API-Secret` 校验

取消中间件中的 Secret 校验。API Key 本身作为身份标识足够（配合 HTTPS），Secret 留作后续 HMAC 签名阶段使用。

**替代方案**: 保留 Secret 校验但改为签名验证 → 工作量大，推迟到下一阶段

### 决策 3：创建时写入明文、创建后清空明文

`CreateApiKeyAsync` 创建时仍生成明文并返回给调用方，但写入数据库时只存哈希。创建完成后只保留 `ApiKeyHash`/`ApiSecretHash`。

**风险**: 现有数据库已有明文数据 → 需要数据迁移清理

### 决策 4：现有数据库明文数据保留不动

不修改现有数据库记录，新创建的 Key 不再存储明文。后续可提供迁移脚本清理。

## Risks / Trade-offs

- **[安全降低]** 移除 Secret 校验后仅靠 API Key 单一因子认证 → 依赖 HTTPS 传输安全，后续需补充 HMAC 签名
- **[兼容性]** 现有外部调用方使用 `X-API-Key` 头的会失败 → **BREAKING CHANGE**，需要文档通知
- **[数据残留]** 数据库已有明文记录 → 不主动清理，新记录不再写入
