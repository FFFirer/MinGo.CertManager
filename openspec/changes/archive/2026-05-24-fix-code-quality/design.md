## Context

代码中存在多个独立的质量问题，各自涉及不同文件但都属于低风险修复，因此合并到一个变更中：

1. **CS8618 警告（9个）**— 属性/字段在退出构造函数时为 null
2. **测试吞异常** — `AcmeServiceIntegrationTests` 中 catch 块不抛出
3. **CRT Content-Type** — `ExternalApiController` 中 CRT 格式返回 `application/x-x509-ca-cert` 应为 `application/zip`
4. **Fire-and-forget** — `Certificates.razor` 中 `_ = LoadCertificates()` 可导致未捕获异常
5. **证书过期时间硬编码** — 使用配置的 `ValidityDays` 而非从证书内容解析 `notAfter`
6. **`AliyunDnsValidationService` 空实现** — 注册了但方法体全空且未被使用

## Goals / Non-Goals

**Goals:**
- 消除所有 CS8618 编译警告
- 测试在断言失败时正确报告
- CRT 下载返回正确的 MIME 类型
- 消除 fire-and-forget 的异步调用模式
- 证书过期时间从证书内容解析
- 移除死代码

**Non-Goals:**
- 不修改任何业务逻辑
- 不改动数据库结构或配置格式
- 不引入新的外部依赖

## Decisions

### 决策 1：CS8618 修复策略

| 文件 | 修复方式 |
|------|---------|
| `CertificateRequest.Domain` | 添加 `= string.Empty` 或 `required` 关键字 |
| `PageHeader.Description` / `Actions` | 添加 `= string.Empty` 或 `[Parameter]` 默认值 |
| `Users.razor` UserManager/RoleManager | 添加 `= null!` 或 `[Inject]` 配合 `= null!` |
| `Profile.razor` 注入属性 | 同上 |
| `HeaderNav.module` | 添加初始化或标记为 `nullable` |

### 决策 2：证书 ExpiresAt 从证书解析

使用 BouncyCastle 的 `X509CertificateParser` 解析 `certificate.CertificateContent` 中的 PEM 证书，读取 `NotAfter` 属性。如果解析失败则回退到 `ValidityDays`。

### 决策 3：测试异常处理

将 `AcmeServiceIntegrationTests` 中的 catch 块改为 `Assert.Fail()` 或直接移除 try-catch（让 xUnit 处理异常）。

## Risks / Trade-offs

- **[证书 ExpiresAt 解析]** 如果 `CertificateContent` 不是有效的 PEM 格式会解析失败 → 回退到现有逻辑
- **[测试修改]** 集成测试可能因外部 ACME 服务不可用而失败 → 在测试方法上添加 `[Trait("Category", "Integration")]` 标记，CI 中可选运行
