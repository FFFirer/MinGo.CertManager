## 1. Web 层：RegisterOpenIdConnectProvider 增加 Realm 拼接

- [x] 1.1 在 `RegisterOpenIdConnectProvider` 中增加 Realm 字段读取和 Authority 拼接逻辑

## 2. Web 层：配置文件更新

- [x] 2.1 更新 `appsettings.json` 中 simpleidserver 配置节，增加 `Realm` 字段示例

## 3. 主 Spec 同步

- [x] 3.1 同步 delta spec 到主 `openspec/specs/oauth-login/spec.md`

## 4. 验证

- [x] 4.1 dotnet build 通过
