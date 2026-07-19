## Context

MinGo.CertManager 是一个 .NET 10 Blazor Server 应用，通过 Kestrel 监听 HTTP 端口（默认 8080）。生产部署中，Kestrel 不直接暴露给公网，而是通过 nginx/Caddy/Traefik 反向代理对外提供 HTTPS 服务。当前没有任何反向代理适配，导致以下问题：

1. **OAuth/OIDC redirect_uri 错误**：ASP.NET Core OAuth 中间件使用 `HttpContext.Request.Scheme` + `Host` 构造回调地址。反代转发后 Scheme 为 `http`、Host 为 `localhost:8080`，与 OAuth Provider 注册的 `https://example.com/signin-{provider}` 不匹配。
2. **HTTPS 重定向循环风险**：`UseHttpsRedirection()` 检测到 `http` 请求时尝试重定向到 `https`，但反代后面的 Kestrel 不会接收到 HTTPS 请求。
3. **Blazor Server SignalR 连接**：`NavigationManager.BaseUri` 基于当前请求的 Scheme/Host，错误的值会导致 SignalR 协商失败或连接到错误端点。

ASP.NET Core 内置 `UseForwardedHeaders()` 中间件可读取反代设置的 `X-Forwarded-For`、`X-Forwarded-Proto`、`X-Forwarded-Host` 头，在管道早期修复请求的 `RemoteIp`、`Scheme` 和 `Host`，使得后续所有中间件和框架代码使用正确的值。

## Goals / Non-Goals

**Goals:**
- 应用可部署在反向代理后面，OAuth 登录、Blazor SignalR、HTTPS 重定向等功能正常运行
- 通过配置控制启用（默认禁用），不影响现有部署
- 遵循项目现有模式：`AppSettings.cs` 配置类 + `services.Configure` + 条件性 `app.UseForwardedHeaders()`
- 只修改应用层代码，不改动任何已有功能代码（OAuth、Blazor、控制器等）

**Non-Goals:**
- 不引入 PathBase 支持（部署在根路径 `/`）
- 不修改或移除 `UseHttpsRedirection()`（在 ForwardedHeaders 修复 scheme 后正常工作）
- 不涉及反向代理本身的配置（如 nginx conf），仅提供参考文档
- 不处理 `Forwarded` 标准头（RFC 7239），仅支持 `X-Forwarded-*` 头（业界事实标准）

## Decisions

### 决策 1：启用方式 — Environment Variable vs appsettings.json

| 方式 | 优点 | 缺点 |
|---|---|---|
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` | 微软文档推荐，Docker 场景一行搞定 | 只控制开/关，无法配置具体头类型 |
| `appsettings.json` + `ForwardedHeaders.Enabled` | 与现有 `OAuthProviders.EnabledProviders` 模式一致 | 多一个配置节 |

**决定**：采用 `appsettings.json` 配置方式。原因：
- 与代码库现有 `EnabledProviders` 模式完全一致
- 后续如需扩展配置（如自定义 `ForwardLimit`）可直接扩展 `ForwardedHeadersSettings` 类
- 环境变量仍可通过 `dotnet` 配置层级覆盖（`ForwardedHeaders__Enabled=true`）

### 决策 2：信任代理范围 — KnownProxies/KnownNetworks

| 方式 | 风险 | 适用场景 |
|---|---|---|
| 默认（仅 loopback） | 安全但 Docker 网络不匹配 | 非容器化部署 |
| `Clear()` 全部信任 | 允许 IP 伪造 | Docker/K8s 中代理 IP 不可预测 |
| 明确指定代理网段 | 最安全 | 固定代理 IP 环境 |

**决定**：在启用时 `Clear()` KnownNetworks/KnownProxies。原因：
- 本项目为 Docker 部署，代理 IP（Docker bridge/gateway）不可预先知道
- 启用 ForwardedHeaders 本身就是显式配置行为，操作者应理解安全含义
- `X-Forwarded-Proto` 伪造的威胁在反代场景中可忽略（内部网络）

### 决策 3：ForwardedHeaders 类型

**决定**：启用 `XForwardedFor | XForwardedProto | XForwardedHost`。原因：
- `XForwardedFor`：修复客户端真实 IP
- `XForwardedProto`：修复 Scheme（最关键，影响 OAuth redirect_uri 和 HTTPS 重定向）
- `XForwardedHost`：修复 Host 头（反代可能改写 Host，如端口号的变更）

### 决策 4：中间件位置

按照 ASP.NET Core 官方文档，`UseForwardedHeaders()` 必须位于管道最顶端，在任何其他中间件之前。

**修改后的中间件顺序：**
```
① UseForwardedHeaders()           ← 新增，最优先
② UseExceptionHandler("/Error")   ← 原位置
③ UseHsts()                       ← 原位置
④ UseViteDevelopmentServer()      ← 原位置
⑤ UseHttpsRedirection()           ← 现在可正常工作
⑥ UseStaticFiles()
⑦ UseRouting()
⑧ UseAuthentication()
⑨ UseAuthorization()
⑩ UseApiKeyAuthentication()
⑪ UseAntiforgery()
⑫ MapControllers() / MapRazorPages() / MapBlazorHub()
```

## Risks / Trade-offs

- **[安全] 信任所有代理 IP → 客户端 IP 可被伪造**：启用时应确保反代不会将 `X-Forwarded-*` 头从外部传入（反代应覆盖而非追加这些头）。缓解：此问题仅影响日志审计，不影响认证授权（OAuth 和 Cookie 认证不依赖 RemoteIp）。
- **[兼容] 非反代环境中误启用**：`ForwardedHeaders.Enabled = true` 但在无反代直连时，外部请求可伪造 `X-Forwarded-Proto` 等头。缓解：默认禁用，需显式配置启用；建议仅在 Docker 反代部署中设置。
- **[回退] 无迁移成本**：仅新增条件性代码，不修改现有逻辑。回退只需移除配置或设为 `false`。
