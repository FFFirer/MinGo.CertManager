# 待后续处理的改进项

本文档记录了在代码审计中发现但暂不处理的可改进项，留待后续版本中实施。

## 分类说明

| 标记 | 含义 |
|------|------|
| 🟡 deferred | 已评估确认暂缓，有具体条件或等待触发 |
| 🔵 planned | 已规划但需先完成前置条件 |
| ⚪ noted | 已记录，低优先级 |

---

## 1. API Key 安全机制完善

**标记**: 🟡 deferred
**优先级**: 中
**触发条件**: 开放平台需要正式对外提供 API 时

### 待实现功能
- **IP 白名单校验**: 在 `ApiKeyAuthenticationMiddleware` 中校验请求来源 IP 是否在 `ApiKey.IpWhitelist` 内
- **时间戳 + Nonce 防重放**: 验证 `X-API-Timestamp` 和 `X-API-Nonce` 头部，防止重放攻击
- **HMAC 签名验证**: 完整实现签名计算和验证流程
- **限流**: 基于 `ApiKey.RateLimitQpm` 实现每分钟请求数限制

### 参考文档
- `docs/apikey技术方案.md` - 完整的设计方案
- `src/MinGo.CertManager.Web/Middleware/ApiKeyAuthenticationMiddleware.cs` - TODO 标记位置

---

## 2. 证书续签导致的重复证书

**标记**: ⚪ noted
**优先级**: 低
**说明**: 当前 `CertificateRenewalJob` 续签时创建新证书记录，旧证书标记为 Expired。同一域名可能出现多条 Active 记录窗口期。当前设计可接受，无需变更。

---

## 3. 数据库迁移清理

**标记**: 🔵 planned
**优先级**: 低
**计划**: 在下一次大版本发布时，将所有迁移压缩为单一基线迁移
**当前状态**: 8 次迁移（16 文件），包含多次字段增删历史

---

## 4. UI/UX 改进

**标记**: ⚪ noted
**优先级**: 低
**待改进项**:
- 证书申请页面 ACME 过程实时状态展示（当前需手动刷新证书列表页）
- 导出模态框加载状态指示
- 导航高亮优化
- 提供 Cloudflare/Dnspod DNS 提供商选项（实体枚举已定义，但前端未实现）

---

## 5. 运维与 DevOps

**标记**: ⚪ noted
**优先级**: 低

### 5.1 Docker compose 配置
- **文件**: `docker-compose.yml`
- **问题**: YAML 语法错误，`services.certmanager.build` 同时使用了短语法和长语法
- **修复**: 统一为一种语法

### 5.2 CI/CD 缺少单元测试步骤
- **文件**: `.gitea/workflows/build.yml`
- **问题**: Docker 构建前没有 `dotnet test` 步骤
- **修复**: 在 build 前添加测试步骤

### 5.3 Dockerfile 多阶段构建优化
- 当前 Dockerfile 可进一步优化分层缓存

---

## 6. SDK 相关问题

**标记**: 🟡 deferred
**优先级**: 中

### 6.1 HttpClient 生命周期管理
- `MinGoCertManagerClient` 未实现 `IDisposable`，自定义 `HttpClient` 路径可能导致资源泄漏

### 6.2 SDK API 端点覆盖
- SDK 只实现了 `RequestCertificate` 和按域名下载，未覆盖按 ID 下载端点

### 6.3 SDK 测试空模板
- `test/MinGo.CertManager.SDK.Tests/UnitTest1.cs` 是空模板，需要真实测试

---

## 7. 代码质量改进

**标记**: ⚪ noted
**优先级**: 低

### 7.1 泛化 Exception
- `AcmeService.cs:237,367` - 使用 `throw new Exception(...)` 而非自定义异常
- `AliyunDnsService.cs:94,133` - 同上

### 7.2 缺少 ConfigureAwait(false)
- 所有非 Web 层（Application/Infrastructure/SDK）的 `await` 调用未使用 `ConfigureAwait(false)`

### 7.3 ACME Service 实例字典可能内存泄漏
- `_orders` 和 `_authorizations` 字典随 `AcmeService` 生命周期存在，高并发下有风险

### 7.4 ParseExpiryDate 异常静默吞掉
- `CertificateService.cs:230-241` - 解析失败时无日志

### 7.5 Mapster 过度依赖
- 只为 `AliyunDnsService` 中一次 `Adapt<>` 调用引入了 Mapster DI 包

### 7.6 测试代码重复
- `CertificateRepositoryTests.cs` 中 7 个测试重复构造 Certificate 对象

---

## 8. 当前环境状态

- **.NET SDK**: 10.0.204 ✅ 已验证可用
- **数据库**: `certmanager.db` 已被 `.gitignore` 排除跟踪 ✅
- **演示项目**: `demos/` 目录未处理
