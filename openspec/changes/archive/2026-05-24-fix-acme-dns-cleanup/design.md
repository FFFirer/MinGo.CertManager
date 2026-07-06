## Context

`AcmeService.RequestCertificateAsync` 的异常处理路径没有清理 DNS 资源。如果 DNS TXT 记录创建成功但后续验证失败，记录会被遗留在阿里云上。另外 `SplitDomainName` 方法假定了根域总是域名最后两段，这不适用于 `co.uk`、`com.cn` 等多段公共后缀。

## Goals / Non-Goals

**Goals:**
- 异常时自动清理已创建的 DNS TXT 记录
- 修复多段 TLD 域名解析为错误的根域名
- `dnsService` 参数使用接口类型而非 `object`

**Non-Goals:**
- 不引入完整的公共后缀列表（PBL）库
- 不改动 Certes 库的使用方式

## Decisions

### 决策 1：DNS 清理采用 try-finally 模式

在 `HandleDnsChallengeAsync` 中使用 `try-finally` 确保无论验证成功或失败都会清理 DNS 记录。

### 决策 2：`SplitDomainName` 采用 3 段启发式

对于 `a.b.c.d` 格式的域名，当段数 >= 4 时取最后 3 段作为根域（`b.c.d`），否则取最后 2 段。

**理由**: 中国常见的 `.com.cn`、`.net.cn`、`.org.cn` 以及常见的 `.co.uk` 都是 3 段公共后缀。这种启发式覆盖大部分通用场景而无需引入外部 PBL 库。

**局限性**: 对于 `a.b.c.org.uk`（4 段公共后缀）这样的域名仍然不准确。但此类域名在真实场景中极为罕见，可以接受。

### 决策 3：`IAcmeService` 接口参数类型从 `object` 改为 `IAliyunDnsService`

当前 `RequestCertificateAsync` 接受 `object dnsService` 然后强制转换为 `IAliyunDnsService`。修改为直接接受接口类型。

## Risks / Trade-offs

- **[SplitDomainName 不精确]** 启发式算法不能覆盖所有 TLD → 当前覆盖足够广泛的中国/国际常见 TLD，后续可替换为公共后缀列表（PBL）库
- **[清理时机]** DNS 记录创建后、验证前如果进程崩溃则仍无法清理 → 需要 TTL 到期自动清理，阿里云默认 TTL=600
