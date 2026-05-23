namespace MinGo.CertManager.Core.Services;

/// <summary>
/// API Key服务接口
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// 创建API Key
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="description">描述</param>
    /// <param name="expiresAt">过期时间</param>
    /// <param name="ipWhitelist">IP白名单</param>
    /// <param name="rateLimitQpm">每分钟限流</param>
    /// <returns>API Key和Secret（明文）</returns>
    Task<(string ApiKey, string ApiSecret, long Id)> CreateApiKeyAsync(long appId, string description, DateTime? expiresAt = null, string? ipWhitelist = null, int rateLimitQpm = 1000);
    
    /// <summary>
    /// 验证API Key（仅校验 Key 是否存在且有效）
    /// </summary>
    /// <param name="apiKey">API Key</param>
    /// <returns>验证结果</returns>
    Task<bool> ValidateApiKeyAsync(string apiKey);

    /// <summary>
    /// 验证API Key和Secret（完整校验，预留用于HMAC签名阶段）
    /// </summary>
    /// <param name="apiKey">API Key</param>
    /// <param name="apiSecret">API Secret</param>
    /// <returns>验证结果</returns>
    Task<bool> ValidateApiKeyAsync(string apiKey, string apiSecret);
    
    /// <summary>
    /// 获取所有API Key
    /// </summary>
    /// <returns>API Key列表</returns>
    Task<List<ApiKeyInfo>> GetAllApiKeysAsync();
    
    /// <summary>
    /// 删除API Key
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteApiKeyAsync(long id);
    
    /// <summary>
    /// 禁用API Key
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>是否禁用成功</returns>
    Task<bool> DisableApiKeyAsync(long id);
    
    /// <summary>
    /// 启用API Key
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>是否启用成功</returns>
    Task<bool> EnableApiKeyAsync(long id);
}

/// <summary>
/// API Key信息
/// </summary>
public class ApiKeyInfo
{
    /// <summary>
    /// ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 应用ID
    /// </summary>
    public long AppId { get; set; }
    
    /// <summary>
    /// API Key掩码（仅显示后8位）
    /// </summary>
    public string ApiKeyMask { get; set; } = string.Empty;
    
    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    public int Status { get; set; }
    
    /// <summary>
    /// IP白名单
    /// </summary>
    public string? IpWhitelist { get; set; }
    
    /// <summary>
    /// 每分钟限流
    /// </summary>
    public int RateLimitQpm { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}
