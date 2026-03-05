---
alwaysApply: true
---
# 配置说明

MinGo.CertManager 支持通过配置文件和环境变量进行配置。

## 配置方式

### 1. 配置文件 (appsettings.json)

应用程序根目录下的 `appsettings.json` 文件包含所有配置项。

### 2. 环境变量

支持通过环境变量覆盖配置文件中的设置。环境变量命名规则：
- 使用双下划线 `__` 作为配置节分隔符
- 例如：`Acme__AccountEmail` 对应 `Acme:AccountEmail`

### 3. 用户机密 (User Secrets)

在开发环境中，可以使用 .NET User Secrets 来存储敏感信息：

```bash
cd src/MinGo.CertManager.Web
dotnet user-secrets init
dotnet user-secrets set "Acme:AccountEmail" "your-email@example.com"
dotnet user-secrets set "AliyunDns:AccessKeyId" "your-access-key-id"
dotnet user-secrets set "AliyunDns:AccessKeySecret" "your-access-key-secret"
```

## 配置项说明

### ACME 配置

| 配置项 | 环境变量 | 说明 | 默认值 |
|---------|-----------|------|---------|
| Acme:UseStaging | Acme__UseStaging | 是否使用 Let's Encrypt 测试环境 | false |
| Acme:AccountEmail | Acme__AccountEmail | ACME 账户邮箱地址 | (空) |
| Acme:LetsEncryptProductionUrl | Acme__LetsEncryptProductionUrl | Let's Encrypt 生产环境URL | https://acme-v02.api.letsencrypt.org/directory |
| Acme:LetsEncryptStagingUrl | Acme__LetsEncryptStagingUrl | Let's Encrypt 测试环境URL | https://acme-staging-v02.api.letsencrypt.org/directory |

### 证书配置

| 配置项 | 环境变量 | 说明 | 默认值 |
|---------|-----------|------|---------|
| Certificate:ValidityDays | Certificate__ValidityDays | 证书有效期（天） | 90 |
| Certificate:RenewalDaysBeforeExpiry | Certificate__RenewalDaysBeforeExpiry | 过期前多少天自动续签 | 30 |

### 阿里云DNS配置

| 配置项 | 环境变量 | 说明 | 默认值 |
|---------|-----------|------|---------|
| AliyunDns:AccessKeyId | AliyunDns__AccessKeyId | 阿里云 Access Key ID | (空) |
| AliyunDns:AccessKeySecret | AliyunDns__AccessKeySecret | 阿里云 Access Key Secret | (空) |
| AliyunDns:RegionId | AliyunDns__RegionId | 阿里云区域ID | cn-hangzhou |
| AliyunDns:ApiVersion | AliyunDns__ApiVersion | 阿里云DNS API版本 | 2015-01-09 |
| AliyunDns:Endpoint | AliyunDns__Endpoint | 阿里云DNS API端点 | https://alidns.aliyuncs.com/ |

### Quartz配置

| 配置项 | 环境变量 | 说明 | 默认值 |
|---------|-----------|------|---------|
| Quartz:SchedulerName | Quartz__SchedulerName | 调度器名称 | MinGo CertManager Scheduler |
| Quartz:SchedulerInstanceId | Quartz__SchedulerInstanceId | 调度器实例ID | MinGo-CertManager-Scheduler |

## 生产环境配置示例

### 使用环境变量（推荐）

```bash
# Windows PowerShell
$env:Acme__AccountEmail="admin@example.com"
$env:AliyunDns__AccessKeyId="your-access-key-id"
$env:AliyunDns__AccessKeySecret="your-access-key-secret"
dotnet run

# Linux/macOS
export Acme__AccountEmail="admin@example.com"
export AliyunDns__AccessKeyId="your-access-key-id"
export AliyunDns__AccessKeySecret="your-access-key-secret"
dotnet run
```

### 使用 appsettings.json

```json
{
  "Acme": {
    "UseStaging": false,
    "AccountEmail": "admin@example.com"
  },
  "AliyunDns": {
    "AccessKeyId": "your-access-key-id",
    "AccessKeySecret": "your-access-key-secret"
  }
}
```

## 安全建议

1. **不要将敏感信息提交到版本控制系统**
   - `appsettings.json` 中的敏感字段应保持为空
   - 使用环境变量或 User Secrets 来设置实际值

2. **使用测试环境进行首次配置**
   - 设置 `Acme:UseStaging` 为 `true` 进行测试
   - 测试成功后再切换到生产环境

3. **保护阿里云凭证**
   - Access Key Secret 具有完全访问权限
   - 建议使用 RAM 子账号并限制权限

4. **定期轮换密钥**
   - 定期更换阿里云 Access Key
   - 监控异常访问日志
