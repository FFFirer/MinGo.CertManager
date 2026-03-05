namespace MinGo.CertManager.Core.Entities;

/// <summary>
/// API Key实体类
/// </summary>
public class ApiKey
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 所属应用/账号ID
    /// </summary>
    public long AppId { get; set; }
    
    /// <summary>
    /// API Key
    /// </summary>
    public string ApiKeyString { get; set; } = string.Empty;
    
    /// <summary>
    /// API Key哈希值（SHA-256）
    /// </summary>
    public string ApiKeyHash { get; set; } = string.Empty;
    
    /// <summary>
    /// API Secret
    /// </summary>
    public string ApiSecretString { get; set; } = string.Empty;
    
    /// <summary>
    /// API Secret哈希值（SHA-256）
    /// </summary>
    public string ApiSecretHash { get; set; } = string.Empty;
    
    /// <summary>
    /// 状态：1=正常 0=禁用 2=已删除
    /// </summary>
    public int Status { get; set; } = 1;
    
    /// <summary>
    /// IP白名单（JSON格式：["1.2.3.4","5.6.7.0/24"]）
    /// </summary>
    public string? IpWhitelist { get; set; }
    
    /// <summary>
    /// 每分钟限流
    /// </summary>
    public int RateLimitQpm { get; set; } = 1000;
    

    
    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 到期时间（可为空表示长期有效）
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}
