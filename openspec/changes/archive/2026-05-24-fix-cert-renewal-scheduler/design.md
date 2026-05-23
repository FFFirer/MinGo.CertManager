## Context

当前 Quartz 配置仅在 `Program.cs` 中初始化了调度器基础服务，但未注册任何作业：

```csharp
builder.Services.AddQuartz(q => {
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
```

`CertificateRenewalJob` 已实现但从未被调度。同时有一个未被使用的 `QuartzJobFactory.cs` 自定义工厂类。

## Goals / Non-Goals

**Goals:**
- 注册 `CertificateRenewalJob` 到 Quartz 调度器，配置每日执行的触发器
- 使用 `CertificateSettings.RenewalDaysBeforeExpiry` 配置值替代硬编码的 30 天
- 移除死代码 `QuartzJobFactory.cs`

**Non-Goals:**
- 不改变证书续签的核心业务逻辑
- 不添加新的续签策略（仍使用扫描到期 + 调用 `RenewCertificateAsync` 的模式）
- 不修改作业的 `[DisallowConcurrentExecution]` 行为

## Decisions

### 决策 1：使用每日固定间隔触发器

使用 `SimpleSchedule` 设置为 24 小时间隔，而非 Cron 表达式。

**理由**: 每日扫描一次对证书续签场景足够，实现简单，无需处理 Cron 时区问题。

### 决策 2：续签提前量从配置读取

`CertificateRenewalJob` 当前硬编码了 `DateTime.UtcNow.AddDays(30)`。改为注入 `IOptions<CertificateSettings>` 读取 `RenewalDaysBeforeExpiry`。

### 决策 3：移除 `QuartzJobFactory`

`UseSimpleTypeLoader()` 已经能使用 DI 容器解析作业类型，自定义的 `QuartzJobFactory` 从未被引用。

## Risks / Trade-offs

- **[漏扫描]** 如果应用在扫描间隔内重启，不会丢失扫描机会（下次启动后立即开始扫描）
- **[并发续签]** `DisallowConcurrentExecution` 确保同一作业实例不会并行执行
