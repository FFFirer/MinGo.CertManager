## 1. 配置 Quartz 作业调度

- [x] 1.1 在 `Program.cs` 的 `AddQuartz` 配置块中注册 `CertificateRenewalJob` 为调度作业
- [x] 1.2 配置每日执行一次的触发器（`SimpleSchedule` 24 小时间隔）
- [x] 1.3 注入 `IOptions<CertificateSettings>` 到 `CertificateRenewalJob` 替代硬编码 30 天

## 2. 清理死代码

- [x] 2.1 删除 `src/MinGo.CertManager.Infrastructure/Quartz/QuartzJobFactory.cs`

## 3. 验证

- [ ] 3.1 启动应用，确认 Quartz 日志显示作业已注册并首次执行（需部署后验证）
- [x] 3.2 确认 `CertificateRenewalJob.Execute` 使用配置的 `RenewalDaysBeforeExpiry` 值
