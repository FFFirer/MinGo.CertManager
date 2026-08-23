下面是一套适用于开放平台的、基于 API Key 的接口认证设计方案，包含整体思路、数据结构、请求流程和安全加固点，可直接落地实现。

---

## 一、总体思路

目标：为第三方开发者提供一套「简单易用 + 安全可控」的开放平台认证机制。

方案核心：

1. 使用 **API Key 作为调用方身份标识**（必选）
2. 结合 **可选的签名机制（HMAC）防篡改、防重放**（推荐）
3. 全程 **HTTPS / TLS 传输**
4. 配合 **限流、IP 白名单、Key 生命周期管理**

---

## 二、API Key 模型设计

### 1. API Key 的构成

建议把对外展示的 Key 分为两个部分（开发者只看到明文）：

- `api_key`：调用方标识，用于快速查到对应的调用账号 / 应用
- `api_secret`：密钥，用于生成签名，不在日志、界面中明文展示（只给开发者一次可见）

示例：

- `api_key`：`ak_live_XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX`
- `api_secret`：`sk_live_XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX`

### 2. 存储表结构（示例）

以关系型数据库为例（PostgreSQL/MySQL 均可）：

```sql
CREATE TABLE api_keys (
    id              BIGINT       PRIMARY KEY AUTO_INCREMENT,
    app_id          BIGINT       NOT NULL,        -- 所属应用/账号
    api_key_hash    CHAR(64)     NOT NULL UNIQUE, -- 对 api_key 做 SHA-256 哈希
    api_secret_hash CHAR(64)     NOT NULL,        -- 对 api_secret 做 SHA-256 哈希
    status          TINYINT      NOT NULL DEFAULT 1, -- 1=正常 0=禁用 2=已删除
    ip_whitelist    TEXT         NULL,            -- JSON 格式：["1.2.3.4","5.6.7.0/24"]
    rate_limit_qpm  INT          NOT NULL DEFAULT 1000, -- 每分钟限流
    env             VARCHAR(16)  NOT NULL DEFAULT 'prod', -- dev/test/prod
    description     VARCHAR(255) NULL,
    created_at      DATETIME     NOT NULL,
    updated_at      DATETIME     NOT NULL,
    expires_at      DATETIME     NULL,            -- 到期时间（可为空表示长期有效）
    last_used_at    DATETIME     NULL
);
```

**关键点：**

- 数据库中只存 **哈希值**，不存明文 Key/Secret
- 后台界面只在创建时展示一次明文，之后仅展示尾号（如 `****H7dJ`）

---

## 三、请求认证方式设计

### 1. 推荐的 HTTP 头部约定

客户端每次请求时，在 HTTP Header 中携带：

| Header           | 作用                          |
|------------------|-------------------------------|
| `X-API-Key`      | 明文 api_key（身份标识）      |
| `X-API-Timestamp`| 时间戳（ISO8601 或 Unix 秒）  |
| `X-API-Nonce`    | 随机字符串，防重放           |
| `X-API-Signature`| 签名（HMAC-SHA256）          |

> 若只需要最简单的方案，可初期只启用 `X-API-Key` 校验；  
> 正式生产强烈建议同时启用签名机制。

### 2. 签名计算规则（推荐）

#### 2.1 待签名字符串

约定服务器和客户端一致的拼接规则，例如：

```text
string_to_sign = timestamp + "\n" +
                 nonce + "\n" +
                 http_method_upper + "\n" +
                 request_path + "\n" +
                 canonical_query_string + "\n" +
                 request_body_md5
```

说明：

- `timestamp`：与 `X-API-Timestamp` 一致
- `nonce`：与 `X-API-Nonce` 一致
- `http_method_upper`：`GET` / `POST` / `DELETE` 等
- `request_path`：不含域名，如 `/v1/order/create`
- `canonical_query_string`：GET 参数按 key 排序，再 `key1=value1&key2=value2` 形式拼接（对 body 传参的 POST 可为空）
- `request_body_md5`：请求体 JSON 字符串做 MD5（空体则固定为 `d41d8cd98f00b204e9800998ecf8427e`）

#### 2.2 签名算法

使用 `api_secret` 作为 HMAC 密钥：

```text
signature = HEX( HMAC-SHA256(secret = api_secret, message = string_to_sign) )
```

将结果放在头部：

```http
X-API-Signature: {signature}
```

### 3. 服务端验证流程（逻辑）

伪代码步骤：

1. **基础参数校验**

   - 检查 `X-API-Key` 是否存在
   - 检查 `Timestamp` / `Nonce` / `Signature` 是否存在（若开启签名模式）

2. **查询 API Key**

   - 对 `X-API-Key` 做 SHA-256 得到 `api_key_hash`
   - 在 `api_keys` 表中匹配并校验：
     - `status == 1`（启用）
     - `expires_at` 未过期（若设置）
     - 环境匹配（如 header 或网关层区分 dev / prod）

3. **IP 白名单校验（可选但推荐）**

   - 读取 `ip_whitelist`
   - 若非空，则只允许来源 IP 在白名单/网段中

4. **时间戳 & Nonce 防重放处理**

   - 检查 `timestamp` 与当前时间差不要超过 N 分钟（如 5 分钟）
   - 使用缓存（如 Redis）记录 `(api_key + nonce)` 一段时间内已用：
     - 若重复出现相同 nonce → 拒绝（防重放）

5. **签名校验**

   - 从数据库中拿到 `api_secret_hash`。此时有两种实现方式：

   **方式 A：服务端可反推出明文 secret（安全性稍弱，不推荐）**

   - 存明文或可逆加密，直接获取 secret 做 HMAC 计算并 `== signature`

   **方式 B：不存明文 secret，仅存哈希（推荐）**

   - 在发放 Key 时，不再在服务端做 HMAC，只依赖客户端正确性 + 时间戳 + Nonce + HTTPS
   - 若必须服务端验证签名，则需要可逆加密存储 secret（加密密钥仅服务端掌握）

   （在你们内部安全策略允许的前提下，可采用 A，实际很多云厂商是 A 方案 + 严格密钥管理）

6. **限流**

   - 基于 `api_key_id` 做 QPS 或 QPM 限流（如 Redis 计数器 / 滑动窗口）
   - 超出返回 `429 Too Many Requests`

7. **通过后，将 app 信息注入上下文**

   - 如 `request.context.app_id = db_record.app_id`
   - 后续业务服务据此做权限和配额控制

---

## 四、错误码与返回约定

建议统一错误规范，方便调用方排查问题。

示例 JSON：

```json
{
  "code": "AUTH_INVALID_SIGNATURE",
  "message": "签名校验失败，请检查签名算法和密钥",
  "request_id": "20250304120100001234567890"
}
```

常见错误码建议：

- `AUTH_MISSING_API_KEY`：缺少 `X-API-Key`
- `AUTH_KEY_NOT_FOUND`：API Key 不存在
- `AUTH_KEY_DISABLED`：API Key 已禁用
- `AUTH_KEY_EXPIRED`：API Key 已过期
- `AUTH_SIGNATURE_REQUIRED`：当前接口必须签名
- `AUTH_INVALID_SIGNATURE`：签名错误
- `AUTH_TIMESTAMP_EXPIRED`：时间戳超出允许范围
- `AUTH_REPLAY_ATTACK`：Nonc e 重放
- `AUTH_IP_NOT_ALLOWED`：IP 不在白名单
- `RATE_LIMIT_EXCEEDED`：超出限流

---

## 五、管理后台和运维能力

### 1. Key 管理功能

- 创建 / 吊销 / 禁用 API Key
- 每个应用可创建多个 Key（用于多环境、多服务拆分）
- 支持设置：
  - 自定义描述
  - IP 白名单
  - 每分钟/每秒请求上限
  - 过期时间

### 2. 安全运维能力

- 全量请求日志中 **禁止写入明文 api_key / api_secret**，只记录 Mask 后的 Key
- 提供 Key 使用监控：
  - 调用量趋势
  - 错误率
  - 异常 IP 分布
- 一旦发现泄露风险，可：
  - 立即禁用指定 Key
  - 发邮件/短信通知使用方
  - 引导生成新 Key 并完成平滑切换

---

## 六、实际落地建议（从简到严）

1. **第一阶段（快速上线）**

   - 强制 HTTPS
   - `X-API-Key` + 简单限流
   - 管理后台支持 Key 的创建/禁用

2. **第二阶段（增强安全）**

   - 加入 IP 白名单机制
   - 加入时间戳 + Nonce 校验
   - 引入签名字段 `X-API-Signature`

3. **第三阶段（企业级）**

   - 实现 Key 轮换策略（例如 90 天轮换，支持老 Key 泄露时快速切换）
   - 针对高风险接口（支付、资产相关），强制签名 + 更严格的限流
   - 监控与告警（异常 IP、短时间内大量 401/403/429）

---

## 七、简要示例（请求示例）

**请求头示例：**

```http
POST /v1/order/create HTTP/1.1
Host: api.example.com
Content-Type: application/json
X-API-Key: ak_live_3UuExy7HBmBqPjF9s4qYZwXc2kN8VrDa
X-API-Timestamp: 2026-03-04T10:00:00Z
X-API-Nonce: 9f3b2a7c1d4e5f60
X-API-Signature: 4b1f8c1c90c7d34c8f8f9b8a6d7e8c9d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6

{"amount":100,"currency":"CNY","subject":"test"}
```

服务端根据前述规则重算签名，比对通过，即视为认证通过。

---

这套方案从「数据结构 → 请求头协议 → 签名算法 → 校验流程 → 管理后台与运维」是闭环的，你可以根据实际业务复杂度，选择先实现核心（API Key + 限流 + HTTPS），再逐步增加签名、防重放和白名单等能力。