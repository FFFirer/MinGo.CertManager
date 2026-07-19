## Why

MinGo.CertManager 部署在 Docker 容器中，生产环境通常通过 nginx/Caddy/Traefik 等反向代理对外提供服务。当前应用不支持反代场景：OAuth/OIDC 登录的 redirect_uri 使用内部 Scheme/Host（http://localhost:8080），导致认证失败；Blazor Server 的 SignalR 连接也不正确处理代理转发。

## What Changes

- 新增 `ForwardedHeadersSettings` 配置类，支持通过配置启用转发表头处理
- 在 `Program.cs` 中条件性注入 `UseForwardedHeaders()` 中间件（默认禁用）
- 在 `appsettings.json` 中添加 `ForwardedHeaders` 配置节
- 支持 `X-Forwarded-For`、`X-Forwarded-Proto`、`X-Forwarded-Host` 三个标准的反代转发表头
- **不引入** PathBase 支持（部署在根路径，无子路径需求）
- **不修改** 任何 OAuth、Blazor、控制器代码 — ForwardedHeaders 自动修复所有 URL 构建

## Capabilities

### New Capabilities
- `reverse-proxy-support`: 支持部署在反向代理后面，正确解析 OAuth redirect_uri、Blazor SignalR 连接 URL、HTTPS 重定向等场景

### Modified Capabilities
<!-- 无现有 spec 的需求变更 — ForwardedHeaders 是基础设施变更，不改变已有功能的需求定义 -->

## Impact

- **修改 3 个文件**：`AppSettings.cs`（新增配置类）、`Program.cs`（注入中间件）、`appsettings.json`（新增配置节）
- **无外部依赖变更**：`UseForwardedHeaders()` 是 ASP.NET Core 内置中间件，无需新增 NuGet 包
- **默认不启用**：现有部署完全不受影响；仅当设置 `ForwardedHeaders.Enabled = true` 时生效
