## 1. 修改中间件认证协议

- [x] 1.1 修改 `ApiKeyAuthenticationMiddleware.InvokeAsync`：将头部检查从 `X-API-Key` 改为 `Authorization: ApiKey <key>` 格式
- [x] 1.2 移除 `X-API-Secret` 头部的检查逻辑
- [x] 1.3 移除 `ApiKeyHeaderName`/`ApiSecretHeaderName` 常量，替换为 `AuthorizationHeaderName` + `ApiKeyScheme`

## 2. 修复 API Key 明文存储

- [x] 2.1 修改 `ApiKeyService.CreateApiKeyAsync`：不再写入 `ApiKeyString`/`ApiSecretString`，仅保留哈希值
- [x] 2.2 修改 `ApiKeyService.GetAllApiKeysAsync`：不再返回明文 `ApiKey` 和 `ApiSecret` 字段
- [x] 2.3 修改 `ApiKeyInfo` 模型：移除明文 `ApiKey`/`ApiSecret` 属性，仅保留 `ApiKeyMask`

## 3. 修复管理页面

- [x] 3.1 修改 `ApiKeys.razor` 列表页：移除 `ApiKey` 和 `ApiSecret` 的明文展示，仅显示掩码
- [x] 3.2 修改 `ApiKeys.razor` 创建逻辑：创建成功后弹窗展示一次明文（保持原有 alert 逻辑）
- [x] 3.3 `MaskApiKey` 使用 `ApiKeyHash` 的后 8 位做掩码（原有逻辑不变）

## 4. 验证

- [x] 4.1 SDK `ApiClient` 发送 `Authorization: ApiKey <key>` 格式与中间件匹配
- [x] 4.2 构建通过，中间件正确解析 `Authorization: ApiKey` 头
