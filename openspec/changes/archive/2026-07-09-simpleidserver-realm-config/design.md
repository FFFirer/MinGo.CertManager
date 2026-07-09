## Context

当前 `RegisterOpenIdConnectProvider` 直接将 `Authority` 配置值赋给 `OpenIdConnectOptions.Authority`。Realm 需要用户手动拼接到 Authority URL 中（如 `https://host/master`）。

SimpleIdServer 的 Realm 可启用（`UseRealm=true`）也可禁用（`UseRealm=false`）。启用时所有 OIDC 端点带有 `{realm}` 路径前缀。OIDC 中间件对 Authority 带路径段（如 `/master`）原生支持——只是拼接到 discovery URL 尾部。

## Goals / Non-Goals

**Goals:**
- 为 SimpleIdServer 配置增加可选 `Realm` 字段
- 代码中自动拼接 Realm 到 Authority（有 Realm 时）
- 向后兼容：已有无 Realm 字段的配置不受影响

**Non-Goals:**
- 不改动 OAuth Provider 配置（GitHub/Google 无 Realm 概念）
- 不改动 ExternalLogin 流程

## Decisions

### Realm 拼接逻辑

```csharp
var authority = section["Authority"] ?? "";
var realm = section["Realm"] ?? "";

options.Authority = string.IsNullOrEmpty(realm)
    ? authority
    : $"{authority.TrimEnd('/')}/{realm}";
```

### 配置格式

```json
"simpleidserver": {
    "Authority": "https://ids.example.com",
    "Realm": "master",       // 可选，默认无
    "ClientId": "...",
    "ClientSecret": "..."
}
```

### 使用场景

| Authority | Realm | 结果 | 场景 |
|-----------|-------|------|------|
| `https://ids.example.com` | `master` | `https://ids.example.com/master` | UseRealm=true + 默认 realm |
| `https://ids.example.com` | `dev` | `https://ids.example.com/dev` | UseRealm=true + 自定义 realm |
| `https://ids.example.com` | 未设置 | `https://ids.example.com` | UseRealm=false，无 realm |
| `https://ids.example.com/custom` | 未设置 | `https://ids.example.com/custom` | 兼容旧配置（realm 已嵌入） |
