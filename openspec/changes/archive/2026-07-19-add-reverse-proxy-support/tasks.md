## 1. 配置层

- [x] 1.1 在 `AppSettings.cs` 中新增 `ForwardedHeadersSettings` 配置类，包含 `Enabled` 属性和 `SectionName` 常量

## 2. 应用配置

- [x] 2.1 在 `appsettings.json` 中新增 `ForwardedHeaders` 配置节，`Enabled` 默认 `false`

## 3. 中间件注册

- [x] 3.1 在 `Program.cs` 的 Service 注册区添加 `services.Configure<ForwardedHeadersSettings>()`
- [x] 3.2 在 `Program.cs` 的中间件管道最顶端（`var app = builder.Build()` 之后立即）添加条件性 `UseForwardedHeaders()` 调用

## 4. 验证

- [x] 4.1 执行 `dotnet build` 确认编译通过
- [x] 4.2 确认 LSP diagnostics 无错误
