## Why

SimpleIdServer 的 Realm 是多租户隔离的核心概念，影响所有 OIDC 端点 URL 路径。当前配置将 Realm 隐式嵌入在 Authority URL 中，不够直观且容易出错。需要添加显式的 Realm 配置项，使对接配置更清晰、可发现。

## What Changes

- 在 SimpleIdServer 配置节新增可选 `Realm` 字段
- `RegisterOpenIdConnectProvider` 中增加 Realm 拼接逻辑：有 Realm 时 `{Authority}/{Realm}`，无 Realm 时直接用 Authority
- 更新 appsettings.json 配置示例
- 更新 oauth-login spec 增加 Realm 配置说明

## Capabilities

### New Capabilities

（无）

### Modified Capabilities

- `oauth-login`: SimpleIdServer 配置节新增可选 `Realm` 字段；`RegisterOpenIdConnectProvider` 增加 Realm 拼接逻辑

## Impact

- `src/.../Web/Extensions/OAuthProviderExtensions.cs` — RegisterOpenIdConnectProvider 增加 Realm 拼接
- `src/.../Web/appsettings.json` — 更新配置示例，增加 Realm 字段
- `openspec/specs/oauth-login/spec.md` — SimpleIdServer 需求 + 配置管理需求更新
