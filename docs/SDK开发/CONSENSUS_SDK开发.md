# SDK开发项目共识文档

## 需求描述
创建一个SDK项目，用于封装开放平台接口调用为方法，并内部处理接口认证。

## 验收标准
1. SDK项目能够独立编译和使用
2. SDK封装了所有开放平台接口调用
3. SDK内部处理API密钥认证
4. SDK提供类型安全的方法调用
5. SDK支持异步调用
6. SDK提供错误处理机制

## 技术实现方案

### 项目结构
- 创建新的SDK项目：`MinGo.CertManager.SDK`
- SDK项目应该是一个独立的类库，不依赖于Web项目
- SDK项目应该引用Core项目，以使用其中的实体和枚举类型

### 核心组件
1. **ApiClient**：核心客户端类，负责处理HTTP请求和认证
2. **Services**：封装开放平台接口的服务类
3. **Models**：SDK特有的数据模型
4. **Exceptions**：SDK特有的异常类

### 认证处理
- SDK内部处理API密钥认证
- 用户需要在初始化SDK时提供API密钥
- SDK在每次请求时自动添加认证头

### 接口封装
SDK需要封装以下接口：
1. **申请新证书**：`POST /api/external/certificates`
2. **下载证书**：`GET /api/external/certificates/{domain}/download`

### 技术约束
- 使用.NET 10
- 使用HttpClient进行HTTP请求
- 使用System.Text.Json进行序列化和反序列化
- 支持异步调用
- 提供详细的错误处理

## 任务边界限制
- SDK仅封装开放平台接口，不包含其他功能
- SDK不依赖于Web项目的具体实现
- SDK应该保持简洁，专注于接口调用和认证处理

## 关键假设
- 开放平台接口的URL是固定的，SDK用户需要在初始化时提供
- API密钥是有效的，SDK不负责验证API密钥的有效性
- 开放平台接口的响应格式是一致的，包含Success和Error字段