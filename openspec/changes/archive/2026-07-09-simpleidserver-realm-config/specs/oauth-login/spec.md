## MODIFIED Requirements

### Requirement: SimpleIdServer OpenID Connect 登录

系统 SHALL 支持使用自建的 SimpleIdServer 作为 OpenID Connect 第三方登录提供程序。

- 使用 OpenID Connect 协议（`AddOpenIdConnect`），通过 `Authority` 自动发现端点
- `AuthenticationType` 为 `OpenIdConnect`
- 回调路径: `/signin-simpleidserver`
- 通过 `Authority` 配置，自动从 `/.well-known/openid-configuration` 发现端点
- **Realm 配置**: 支持通过 `Realm` 配置项指定 SimpleIdServer Realm（如 `"master"`），代码自动拼接为 `{Authority}/{Realm}`
- **向后兼容**: `Realm` 为可选字段，未设置时 `Authority` 原样使用
- 默认 scope: `openid`、`profile`、`email`
- Claim 映射: 通过 OIDC 中间件内置 `ClaimActions` + `GetClaimsFromUserInfoEndpoint`
- 图标: `fas fa-id-card`
- SimpleIdServer 服务端需配置 Redirect URI 为应用地址的 `/signin-simpleidserver`

#### Scenario: 带 Realm 的 SimpleIdServer 配置
- **WHEN** `appsettings.json` 中 `simpleidserver.Authority` 设为 `"https://ids.example.com"` 且 `Realm` 设为 `"master"`
- **THEN** 系统 SHALL 使用 `https://ids.example.com/master` 作为 OIDC Authority 进行端点发现
- **WHEN** OIDC 中间件发起 discovery 请求
- **THEN** 请求 URL 为 `https://ids.example.com/master/.well-known/openid-configuration`

#### Scenario: 无 Realm 的 SimpleIdServer 配置
- **WHEN** `appsettings.json` 中 `simpleidserver.Realm` 未设置或为空
- **THEN** 系统 SHALL 使用 `Authority` 原值作为 OIDC Authority，不做拼接

### Requirement: 配置管理（扩展 Realm 支持）

OAuth/OIDC Provider 的 ClientId 和 ClientSecret SHALL 通过配置管理。OIDC Provider 额外支持 Realm 配置。

- `appsettings.json` 定义 `OAuthProviders` 配置节
- `EnabledProviders` 数组控制启用哪些 Provider 及其显示顺序
- 每个 Provider 的 `DisplayName` 可在配置中覆盖
- OIDC Provider 支持可选的 `Realm` 字段，自动拼接为 `{Authority}/{Realm}`
- 每个 Provider 的 `ClientId` 和 `ClientSecret` 通过 User Secrets 注入
