## 1. 修改 ACME 账号恢复逻辑

- [x] 1.1 修改 `AcmeService.cs` 中缓存命中分支：使用 `new AcmeContext(acmeUri, _accountKey)` 构造器替代无参构造
- [x] 1.2 使用 `_acmeContext.Account()` 替代 `NewAccount()` 来恢复已有账号
- [x] 1.3 在缓存未命中（创建新账号）分支，保持原有 `NewAccount()` 逻辑不变
- [x] 1.4 确保 `_accountKey` 在缓存命中分支被正确赋值并在 `AcmeContext` 构造中使用（同时在新建分支使用 `_acmeContext.AccountKey` 获取真实 Key）

## 2. 验证

- [x] 2.1 执行现有单元测试确认不破坏已有逻辑（2个预先存在的测试失败与本次变更无关：SDK required 属性 + CleanupAsync 验证了错误的方法名）
- [ ] 2.2 在测试环境中验证：首次申请创建账号并缓存，第二次申请复用缓存账号（需部署后手工验证）
