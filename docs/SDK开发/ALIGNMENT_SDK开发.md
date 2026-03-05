# SDK开发项目对齐文档

## 项目上下文分析

### 现有项目结构
- 项目使用ASP.NET Core Blazor，渲染模式为Auto
- 技术栈：EntityFrameworkCore、SQLite、Blazor、TailwindCSS v4、Quartz.NET
- 现有项目结构：
  - MinGo.CertManager.Core：核心实体和常量
  - MinGo.CertManager.Infrastructure：基础设施，包括数据访问、服务等
  - MinGo.CertManager.Web：Web应用
  - MinGo.CertManager.Tests：测试项目

### 现有相关组件
- 已存在ApiKey实体和相关服务（IApiKeyService、ApiKeyService）
- 已实现API密钥认证中间件（ApiKeyAuthenticationMiddleware）
- 已存在ExternalApiController，可能包含开放平台接口

## 需求理解确认

### 原始需求
- 创建一个SDK项目，用于封装开放平台接口调用为方法
- 内部处理接口认证

### 边界确认
- SDK项目应该独立于Web项目
- SDK应该封装所有开放平台接口调用
- SDK内部处理认证逻辑，使用者无需关心认证细节
- SDK应该提供简洁的方法调用接口

### 需求理解
- 需要创建一个新的SDK项目，作为独立的类库
- SDK需要封装现有的开放平台接口调用
- SDK需要内部处理API密钥认证
- SDK应该提供类型安全的方法调用

### 疑问澄清
- 开放平台接口的具体范围是什么？是否仅包含现有的ExternalApiController中的接口？
- SDK的目标用户是谁？是内部开发团队还是外部第三方开发者？
- SDK是否需要支持不同的认证方式，还是仅支持API密钥认证？
- SDK是否需要支持异步调用？
- SDK是否需要提供错误处理和重试机制？