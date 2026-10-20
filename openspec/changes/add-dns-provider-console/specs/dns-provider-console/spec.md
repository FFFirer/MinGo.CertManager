## ADDED Requirements

### Requirement: 内置 DNS 供应商目录

系统 SHALL 提供 DNS 供应商目录，展示内置供应商类型及其可实现状态。

- 目录 SHALL 以代码内置注册表为源，不依赖数据库
- 阿里云 DNS SHALL 标记为已内置实现
- Cloudflare、DNSPod SHALL 以「规划中」占位出现，且不可执行连接体检
- 目录 SHALL 显示供应商展示名与实现状态

#### Scenario: 展示内置与规划中供应商

- **WHEN** 用户打开 DNS 供应商控制台
- **THEN** 系统 SHALL 展示阿里云 DNS（内置）以及 Cloudflare、DNSPod（规划中）

#### Scenario: 规划中供应商不可体检

- **WHEN** 供应商实现状态为规划中
- **THEN** 系统 SHALL 不提供针对该供应商的连接体检操作

### Requirement: 配置态感知与凭据脱敏

系统 SHALL 展示已内置供应商的凭据配置状态，且不得回显 AccessKeySecret 明文。

- 「已配置」判定 SHALL 为 AccessKeyId 与 AccessKeySecret 均非空
- AccessKeyId SHALL 脱敏展示（仅保留前缀，其余以掩码替代）
- AccessKeySecret SHALL 仅展示「已提供 / 未提供」，永不回显
- RegionId、Endpoint 等非密配置 SHALL 可展示
- 配置来源 SHALL 标明为对应配置节（如 `AliyunDns`），并提供配置写法指引

#### Scenario: 已配置脱敏展示

- **WHEN** AliyunDns 配置节的 AccessKeyId 与 AccessKeySecret 均非空
- **THEN** 系统 SHALL 显示状态为已配置，AccessKeyId 以脱敏形式展示，Secret 仅显示已提供

#### Scenario: 未配置展示

- **WHEN** AccessKeyId 或 AccessKeySecret 任一为空
- **THEN** 系统 SHALL 显示状态为未配置，并给出配置指引入口

#### Scenario: Secret 不回显

- **WHEN** 用户查看供应商详情
- **THEN** 系统 SHALL 不输出 AccessKeySecret 明文

### Requirement: 连接体检

系统 SHALL 支持对已内置且已配置的供应商执行只读连接体检。

- 体检 SHALL 通过提供商客户端的已购域名列表能力（只读）完成
- 体检 SHALL 仅由用户主动触发，不做自动轮询
- 体检 SHALL 不执行任何 DNS 记录的创建或删除
- 体检结果 SHALL 区分：连接正常（含已购域名数量）、鉴权失败、连接失败（网络/超时/其它）
- 未配置凭据时 SHALL 禁止体检并提示先完成配置
- 体检结果 SHALL 不写入操作流水

#### Scenario: 体检成功

- **WHEN** 已配置有效凭据且用户触发连接体检
- **THEN** 系统 SHALL 显示连接正常，并给出检出的已购域名数量

#### Scenario: 体检鉴权失败

- **WHEN** 凭据无效导致供应商 API 鉴权失败
- **THEN** 系统 SHALL 显示鉴权失败，并提示检查 AccessKeyId/Secret

#### Scenario: 未配置禁止体检

- **WHEN** 供应商配置态为未配置且用户触发连接体检
- **THEN** 系统 SHALL 不调用供应商 API，并提示先完成凭据配置

### Requirement: 控制台导航与权限

系统 SHALL 提供「DNS 供应商」控制台页面，登录用户可访问。

- 页面 SHALL 位于顶级导航，与域名管理入口并列
- 访问控制 SHALL 为已登录用户，不要求管理员角色
- 申请证书页 SHALL 将 DNS 提供商展示为只读说明并链接到该控制台，不伪装为可编辑下拉

#### Scenario: 导航入口

- **WHEN** 已登录用户查看主导航
- **THEN** 系统 SHALL 展示「DNS 供应商」入口

#### Scenario: 申请证书页引导

- **WHEN** 用户打开申请证书页
- **THEN** 系统 SHALL 只读展示当前 DNS 提供商与凭据来源，并提供到 DNS 供应商控制台的链接

### Requirement: 同步前配置态提示

域名资产同步在凭据未配置时 SHALL 给出明确前置提示，而非透传供应商 API 异常。

#### Scenario: 未配置时同步被引导

- **WHEN** 用户在凭据未配置时触发「从提供商同步」
- **THEN** 系统 SHALL 提示先完成 DNS 供应商凭据配置，并给出控制台入口
