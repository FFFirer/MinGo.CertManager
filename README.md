# MinGo.CertManager

基于 .NET 10 的 HTTPS 证书管理系统，支持自动申请、续签和导出证书。

## 功能特性

- 按域名申请免费的HTTPS证书
  - 支持多域名
  - 支持泛域名
- 证书申请后自动续签
- 证书导出功能
  - 支持 PFX/P12 格式
  - 支持 PEM 格式
  - 支持 CRT/CER 格式
- 证书列表展示
  - 证书状态
  - 申请域名
  - 过期时间
  - 查询条件（证书状态、域名模糊搜索）
  - 导出操作
- 使用 ACME 流程申请证书
  - 使用 DNS01 进行域名所有权验证
  - DNS 管理提供商支持阿里云

## 技术栈

- .NET 10
- EntityFrameworkCore
- SQLite
- Blazor Server
- Bootstrap
- Quartz.NET
- BouncyCastle.Cryptography

## 项目结构

```
MinGo.CertManager/
├── docs/                    # 文档目录
│   ├── ARCHITEURE.md        # 项目架构
│   └── REQUIREMENTS.md       # 需求文档
├── src/                     # 源码目录
│   ├── MinGo.CertManager.Core/           # 核心实体
│   ├── MinGo.CertManager.Infrastructure/ # 基础设施
│   ├── MinGo.CertManager.Application/    # 应用层
│   └── MinGo.CertManager.Web/          # Web 前端
├── test/                    # 测试目录
│   └── MinGo.CertManager.Tests/
└── MinGo.CertManager.sln   # 解决方案文件
```

## 开发规范

- 目标框架使用 .NET 10
- 使用 git 管理仓库代码变更
- 重点功能使用单元测试进行功能覆盖测试
  - 测试框架使用 xUnit
  - 测试数据使用工具生成
- 前端使用 Blazor 进行构建
- 使用解决方案文件组织
- 代码格式使用 editorconfig
- 重复实现需要重构成单一方法，避免重复实现相同逻辑
- 不使用魔数，定义常量
- 必要代码需要注释
- JSON 序列化使用 System.Text.Json
- 先理解需求，再规划技术方案，拆解实现任务，逐步实现，重点逻辑需要单元测试，统计开发进度，回顾并改进，直至实现需求目标
- 补全项目必要文件

## 快速开始

### 前置要求

- .NET 10 SDK
- Visual Studio 2022 或 Rider

### 构建项目

```bash
dotnet build
```

### 运行项目

```bash
cd src/MinGo.CertManager.Web
dotnet run
```

### 运行测试

```bash
dotnet test
```

## 数据库迁移

```bash
dotnet ef migrations add InitialCreate --project src/MinGo.CertManager.Infrastructure --startup-project src/MinGo.CertManager.Web
dotnet ef database update --project src/MinGo.CertManager.Infrastructure --startup-project src/MinGo.CertManager.Web
```

## 配置阿里云DNS

### 使用User Secrets配置（推荐）

在开发环境中，推荐使用.NET User Secrets来存储敏感信息：

```bash
cd src/MinGo.CertManager.Web
dotnet user-secrets init
dotnet user-secrets set "AliyunDns:AccessKeyId" "your-access-key-id"
dotnet user-secrets set "AliyunDns:AccessKeySecret" "your-access-key-secret"
dotnet user-secrets set "Acme:AccountEmail" "your-email@example.com"
```

### 使用环境变量配置

在生产环境中，可以使用环境变量来配置：

```bash
# Windows PowerShell
$env:AliyunDns__AccessKeyId="your-access-key-id"
$env:AliyunDns__AccessKeySecret="your-access-key-secret"
$env:Acme__AccountEmail="your-email@example.com"
dotnet run

# Linux/macOS
export AliyunDns__AccessKeyId="your-access-key-id"
export AliyunDns__AccessKeySecret="your-access-key-secret"
export Acme__AccountEmail="your-email@example.com"
dotnet run
```

## 许可证

MIT License
