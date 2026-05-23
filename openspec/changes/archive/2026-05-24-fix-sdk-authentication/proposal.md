## Why

SDK 认证机制存在三个严重问题：1) SDK 使用 `Authorization: ApiKey xxx` 头发送 API Key，但中间件检查的是 `X-API-Key` 头，导致 SDK 所有 API 调用返回 401；2) API Key 的 Secret 每次请求都明文传输（`X-API-Secret`），等效于每次请求都发送密码；3) 数据库中同时存储了 API Key/Secret 明文和哈希值，违反安全设计文档中"只存哈希"的原则。

## What Changes

- **统一认证头部协议**：中间件改为检查 `Authorization: ApiKey <key>` 头，与 SDK 发送方式匹配
- **移除 `X-API-Secret` 传输**：停止每次请求传输 Secret，改为仅使用 API Key 做身份标识
- **API Key 管理页面安全加固**：创建 API Key 后仅展示一次明文，列表页只显示掩码
- **移除数据库中明文存储**：`ApiKey.ApiKeyString` 和 `ApiSecretString` 字段不再写入明文
- `ApiKeyInfo` 返回模型移除明文 Secret 字段

**BREAKING**: 外部 API 认证协议变更

## Capabilities

### New Capabilities
- `api-key-auth`: 开放平台 API Key 认证机制，支持通过 `Authorization: ApiKey` 头进行身份认证

### Modified Capabilities

（无）

## Impact

- `src/MinGo.CertManager.Web/Middleware/ApiKeyAuthenticationMiddleware.cs` — 改为检查 `Authorization` 头，移除 Secret 校验
- `src/MinGo.CertManager.SDK/ApiClient.cs` — 无变更（已验证发送格式正确）
- `src/MinGo.CertManager.Application/Services/ApiKeyService.cs` — `CreateApiKeyAsync` 不再存储明文；`GetAllApiKeysAsync` 不返回明文 Secret
- `src/MinGo.CertManager.Core/Entities/ApiKey.cs` — `ApiKeyString`/`ApiSecretString` 标记为内部使用或移除
- `src/MinGo.CertManager.Web/Pages/ApiKeys.razor` — 列表页显示掩码，Secret 仅在创建时弹出展示
