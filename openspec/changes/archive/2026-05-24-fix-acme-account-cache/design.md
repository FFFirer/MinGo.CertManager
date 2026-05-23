## Context

`AcmeService` 在证书申请时使用 Certes 库与 Let's Encrypt ACME 服务交互。当前流程：

1. 首次申请：`NewAccount()` → 缓存 `AccountKey` 的 DER 格式到数据库
2. 后续申请：`GetCachedAccountAsync()` 返回缓存 → `KeyFactory.FromDer()` 恢复 Key → 仍然 `NewAccount()`（bug 所在）
3. `NewAccount()` 无论是否有 Key 都创建新账号，忽略了恢复的 Key

## Goals / Non-Goals

**Goals:**
- 缓存命中时正确恢复已有 ACME 账号，不再重复注册
- 缓存未命中时保持现有创建新账号的逻辑
- 保持与 Let's Encrypt 的现有交互模式不动

**Non-Goals:**
- 不改变 ACME 账号缓存的数据结构或存储方式
- 不改动 `AcmeAccountCache` 接口
- 不涉及 ACME 流程其他环节的优化

## Decisions

### 决策 1：使用 `AcmeContext(AcmeUri, IKey)` 构造器恢复账号

**方案**: 缓存命中时，用恢复的 Key 构造 `AcmeContext`，然后调用 `_acmeContext.Account()` 恢复已有账号关联。

```csharp
// 缓存命中时
_acmeContext = new AcmeContext(acmeUri, _accountKey);
var account = await _acmeContext.Account();  // 恢复已有账号
```

**理由**: Certes 文档中 `AcmeContext(Uri, IKey)` 构造器接受已有 Key，`Account()` 方法恢复该 Key 对应的 ACME 账号。这是 Certes 提供的标准恢复方式。

**替代方案考虑**:
- `NewAccount()` + `Key` 参数 — Certes 文档不推荐此方式，仍会创建新账号
- 手动构造 Account 对象 — 过于复杂，不符合 Certes 设计意图

### 决策 2：无需修改缓存数据结构

当前 `AcmeAccount` 实体存储的 `AccountKey`（Base64 编码的 DER 格式 Key）已经是恢复所需全部信息，数据结构无需变更。

## Risks / Trade-offs

- **[兼容性]** Certes 库版本变更可能导致 `Account()` API 行为变化 → 依赖已锁定版本，升级时需回归测试
- **[账号过期]** 远程 ACME 账号可能已被吊销或过期 → `Account()` 会抛出异常，走 catch 分支正常处理
