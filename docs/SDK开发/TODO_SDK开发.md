# SDK开发项目待办事项

## 待办事项

### 1. 单元测试
- [ ] 添加单元测试，确保SDK的稳定性
- [ ] 测试认证功能
- [ ] 测试接口调用功能
- [ ] 测试错误处理机制

### 2. 文档完善
- [ ] 完善SDK文档，提供更详细的使用示例
- [ ] 添加API参考文档
- [ ] 添加常见问题解答

### 3. 功能增强
- [ ] 实现重试机制，提高SDK的可靠性
- [ ] 添加日志记录，便于调试和问题排查
- [ ] 考虑添加更多的接口封装
- [ ] 实现缓存机制，减少重复请求

### 4. 性能优化
- [ ] 优化HTTP请求处理
- [ ] 优化序列化和反序列化
- [ ] 考虑使用HttpClientFactory管理HttpClient生命周期

## 配置说明

### 1. API密钥配置
- SDK用户需要在初始化时提供API密钥
- API密钥应该从安全的地方获取，如环境变量或配置文件
- 示例：
  ```csharp
  // 从环境变量获取API密钥
  var apiKey = Environment.GetEnvironmentVariable("MINGO_API_KEY");
  var client = new MinGoCertManagerClient(apiKey, "https://api.example.com");
  ```

### 2. BaseUrl配置
- SDK用户需要在初始化时提供BaseUrl
- BaseUrl应该指向开放平台API的根地址
- 示例：
  ```csharp
  // 生产环境
  var client = new MinGoCertManagerClient("your-api-key", "https://api.example.com");
  
  // 开发环境
  var client = new MinGoCertManagerClient("your-api-key", "https://dev-api.example.com");
  ```

## 操作指引

### 1. 安装SDK
- 将SDK项目添加为引用
- 或者将SDK打包为NuGet包，通过NuGet安装

### 2. 初始化SDK
- 提供API密钥和BaseUrl
- 可选：提供自定义的HttpClient

### 3. 使用SDK
- 调用CertificateService的方法申请和下载证书
- 处理可能的异常

### 4. 错误处理
- 捕获SdkException及其派生类
- 根据异常类型进行不同的处理

### 5. 最佳实践
- 为每个应用创建一个SDK实例
- 妥善保管API密钥
- 定期轮换API密钥
- 使用测试环境进行测试