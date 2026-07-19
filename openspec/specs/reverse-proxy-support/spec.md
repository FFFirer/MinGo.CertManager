## Purpose

支持将 MinGo.CertManager 部署在 nginx/Caddy/Traefik 等反向代理后面。通过 ASP.NET Core 内置的 ForwardedHeaders 中间件正确解析反代转发的 `X-Forwarded-For`、`X-Forwarded-Proto`、`X-Forwarded-Host` 头，修复 OAuth 登录 redirect_uri、Blazor Server SignalR 连接和 HTTPS 重定向等场景。

## Requirements

### Requirement: 按配置启用 ForwardedHeaders 中间件

系统 SHALL 支持通过配置控制是否启用 ASP.NET Core ForwardedHeaders 中间件。

- 默认 SHALL 禁用，不影响现有部署
- 当 `ForwardedHeaders.Enabled` 为 `true` 时 SHALL 启用中间件
- 配置 SHALL 支持通过 `appsettings.json` 和环境变量（`ForwardedHeaders__Enabled=true`）两种方式设置
- 启用时 SHALL 处理 `X-Forwarded-For`、`X-Forwarded-Proto`、`X-Forwarded-Host` 三个标准转发表头
- 启用时 SHALL 清除 `KnownProxies` 和 `KnownNetworks` 限制以兼容 Docker 网络
- `UseForwardedHeaders()` SHALL 在中间件管道的起始位置注册，早于所有其他中间件

#### Scenario: 默认禁用
- **WHEN** 未设置 `ForwardedHeaders.Enabled`
- **THEN** ForwardedHeaders 中间件 SHALL 不注册
- **THEN** 应用行为与修改前完全一致

#### Scenario: 通过 appsettings.json 启用
- **WHEN** `appsettings.json` 中 `ForwardedHeaders.Enabled` 设为 `true`
- **THEN** 应用启动时 SHALL 注册 `UseForwardedHeaders()` 中间件
- **THEN** 中间件 SHALL 处理 `X-Forwarded-For`、`X-Forwarded-Proto`、`X-Forwarded-Host` 头

#### Scenario: 通过环境变量启用
- **WHEN** 环境变量 `ForwardedHeaders__Enabled` 设为 `true`
- **THEN** 应用启动时 SHALL 注册 `UseForwardedHeaders()` 中间件

### Requirement: 反向代理部署下 OAuth 登录正常运行

系统部署在反向代理后面时 SHALL 保证 OAuth/OIDC 登录流程的 redirect_uri 使用外部公网地址。

- OAuth callback 的 scheme SHALL 使用反代转发的 `X-Forwarded-Proto` 头（通常是 `https`）
- OAuth callback 的 host SHALL 使用反代转发的 `X-Forwarded-Host` 头（通常是外部域名）

#### Scenario: OAuth redirect_uri 使用外部 scheme
- **WHEN** 反向代理设置 `X-Forwarded-Proto: https`
- **WHEN** OAuth 登录发起 Challenge
- **THEN** 发送到 OAuth Provider 的 redirect_uri SHALL scheme 为 `https`

#### Scenario: OAuth redirect_uri 使用外部 host
- **WHEN** 反向代理设置 `X-Forwarded-Host: cert.example.com`
- **WHEN** OAuth 登录发起 Challenge
- **THEN** 发送到 OAuth Provider 的 redirect_uri SHALL host 为 `cert.example.com`

### Requirement: 反向代理部署下 Blazor Server 正常运行

系统部署在反向代理后面时 SHALL 保证 Blazor Server 的 SignalR 连接使用正确的 URL。

- Blazor Server hub 路径 SHALL 保持默认的 `/_blazor`
- SignalR 协商连接的 scheme/host SHALL 使用反代转发的头信息
- `<base href="~/" />` SHALL 保持不变（部署在根路径）

#### Scenario: SignalR 连接使用外部 URL
- **WHEN** 反向代理设置 `X-Forwarded-Proto: https` 和 `X-Forwarded-Host: cert.example.com`
- **WHEN** Blazor 客户端建立 SignalR 连接
- **THEN** SignalR 协商请求 SHALL 使用 `https://cert.example.com/_blazor`

### Requirement: 反向代理部署下 HTTPS 重定向正常

系统部署在反向代理后面时 `UseHttpsRedirection()` 中间件 SHALL 不产生重定向循环。

- `UseForwardedHeaders()` 必须在 `UseHttpsRedirection()` 之前注册
- 当 `X-Forwarded-Proto: https` 时 `UseHttpsRedirection()` SHALL 不触发重定向

#### Scenario: HTTPS 重定向不循环
- **WHEN** 反向代理发送 `X-Forwarded-Proto: https`
- **WHEN** 请求到达 `UseHttpsRedirection()` 中间件
- **THEN** SHALL 不执行重定向，请求正常继续

#### Scenario: 非加密请求仍被重定向
- **WHEN** 请求不经过反向代理（直连）
- **WHEN** 请求使用 HTTP
- **THEN** `UseHttpsRedirection()` SHALL 仍正常重定向到 HTTPS
