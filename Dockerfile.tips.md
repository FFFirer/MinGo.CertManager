# Dockerfile 构建框架说明

## 构建框架概述

本项目使用多阶段构建（Multi-stage Build）策略，分为三个主要阶段：

1. **前端构建阶段**：使用 Node.js 环境构建前端资源（TailwindCSS + Vite）
2. **后端构建阶段**：使用 .NET SDK 构建 .NET 应用
3. **运行时阶段**：使用 .NET ASP.NET Core 运行时镜像运行应用

## 构建步骤

### 1. 构建镜像

```bash
# 基本构建命令
docker build -t mingocertmanager:latest .

# 自定义 NPM 镜像源构建
docker build --build-arg NPM_REGISTRY=https://registry.npmmirror.com -t mingocertmanager:latest .
```

### 2. 运行容器

```bash
# 基本运行命令
docker run -d -p 8080:8080 --name certmanager mingocertmanager:latest

# 挂载数据目录并设置环境变量
docker run -d -p 8080:8080 \
  --name certmanager \
  -v ./data:/app/data \
  -e Acme__AccountEmail=your-email@example.com \
  -e AliyunDns__AccessKeyId=your-access-key-id \
  -e AliyunDns__AccessKeySecret=your-access-key-secret \
  mingocertmanager:latest
```

### 3. 使用 Docker Compose

项目已包含 `docker-compose.yml` 文件，可直接使用：

```bash
docker-compose up -d
```

## 环境变量配置

| 环境变量 | 说明 | 默认值 |
|---------|------|--------|
| ASPNETCORE_URLS | 应用监听地址 | http://+:8080 |
| ASPNETCORE_ENVIRONMENT | 运行环境 | Production |
| Acme__UseStaging | 是否使用 Let's Encrypt 测试环境 | false |
| Acme__AccountEmail | ACME 账户邮箱 | 无 |
| AliyunDns__AccessKeyId | 阿里云 Access Key ID | 无 |
| AliyunDns__AccessKeySecret | 阿里云 Access Key Secret | 无 |

## 目录结构说明

- `/app/data`：应用数据目录，用于存储数据库文件和证书

## 注意事项

1. **首次运行**：建议先设置 `Acme__UseStaging=true` 使用测试环境，确保配置正确后再切换到生产环境

2. **数据持久化**：使用 `docker-compose` 或 `-v` 参数挂载数据目录，确保数据不会丢失

3. **环境变量安全**：敏感信息如 Access Key 应通过环境变量传递，不要硬编码在配置文件中

4. **构建优化**：Dockerfile 已优化缓存策略，修改代码后重新构建会利用缓存，提高构建速度

5. **网络访问**：确保容器能够访问互联网，以便与 Let's Encrypt 和阿里云 API 通信

## 故障排除

### 构建失败

- 检查网络连接是否正常
- 确认 npm 包和 nuget 包能够正常下载
- 检查代码是否存在编译错误

### 运行失败

- 检查环境变量是否正确设置
- 查看容器日志：`docker logs certmanager`
- 确认端口 8080 未被占用

### 证书申请失败

- 检查阿里云 Access Key 是否正确
- 确认域名所有权（DNS 验证）
- 查看应用日志获取详细错误信息

## 性能优化

1. **使用缓存**：Docker 会缓存构建步骤，修改代码后重新构建会更快

2. **减少镜像大小**：多阶段构建已确保运行时镜像最小化

3. **资源限制**：根据实际需求设置容器的内存和 CPU 限制

```bash
docker run -d -p 8080:8080 \
  --name certmanager \
  --memory=512m \
  --cpus=1 \
  mingocertmanager:latest
```