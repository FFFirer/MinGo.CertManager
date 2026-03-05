using System.Security.Cryptography;
using System.Text;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Repositories;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// API Key服务实现
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _apiKeyRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiKeyRepository">API Key仓库</param>
    public ApiKeyService(IApiKeyRepository apiKeyRepository)
    {
        _apiKeyRepository = apiKeyRepository;
    }

    /// <summary>
    /// 创建API Key
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="description">描述</param>
    /// <param name="expiresAt">过期时间</param>
    /// <param name="ipWhitelist">IP白名单</param>
    /// <param name="rateLimitQpm">每分钟限流</param>
    /// <returns>API Key和Secret（明文）</returns>
    public async Task<(string ApiKey, string ApiSecret, long Id)> CreateApiKeyAsync(long appId, string description, DateTime? expiresAt = null, string? ipWhitelist = null, int rateLimitQpm = 1000)
    {
        // 生成API Key
        var apiKey = GenerateApiKey();
        var apiSecret = GenerateApiSecret();

        // 计算哈希值
        var apiKeyHash = ComputeHash(apiKey);
        var apiSecretHash = ComputeHash(apiSecret);

        // 创建API Key实体
        var apiKeyEntity = new ApiKey
        {
            AppId = appId,
            ApiKeyString = apiKey,
            ApiKeyHash = apiKeyHash,
            ApiSecretString = apiSecret,
            ApiSecretHash = apiSecretHash,
            Status = 1, // 1=正常
            IpWhitelist = ipWhitelist,
            RateLimitQpm = rateLimitQpm,
            Description = description,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 保存到数据库
        var createdApiKey = await _apiKeyRepository.CreateAsync(apiKeyEntity);

        return (apiKey, apiSecret, createdApiKey.Id);
    }

    /// <summary>
    /// 验证API Key
    /// </summary>
    /// <param name="apiKey">API Key</param>
    /// <param name="apiSecret">API Secret</param>
    /// <returns>验证结果</returns>
    public async Task<bool> ValidateApiKeyAsync(string apiKey, string apiSecret)
    {
        // 计算哈希值
        var apiKeyHash = ComputeHash(apiKey);
        var apiSecretHash = ComputeHash(apiSecret);

        // 查询API Key
        var apiKeyEntity = await _apiKeyRepository.GetByApiKeyHashAsync(apiKeyHash);
        if (apiKeyEntity == null)
        {
            return false;
        }

        // 检查状态
        if (apiKeyEntity.Status != 1)
        {
            return false;
        }

        // 检查过期时间
        if (apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value < DateTime.UtcNow)
        {
            return false;
        }

        // 检查Secret哈希
        if (apiKeyEntity.ApiSecretHash != apiSecretHash)
        {
            return false;
        }

        // 更新最后使用时间
        await _apiKeyRepository.UpdateLastUsedAsync(apiKeyEntity.Id);

        return true;
    }

    /// <summary>
    /// 获取所有API Key
    /// </summary>
    /// <returns>API Key列表</returns>
    public async Task<List<ApiKeyInfo>> GetAllApiKeysAsync()
    {
        var apiKeys = await _apiKeyRepository.GetAllAsync();
        return apiKeys.Select(key => new ApiKeyInfo
        {
            Id = key.Id,
            AppId = key.AppId,
            ApiKeyMask = MaskApiKey(key.ApiKeyHash),
            ApiKey = key.ApiKeyString,
            ApiSecret = key.ApiSecretString,
            Description = key.Description,
            Status = key.Status,
            IpWhitelist = key.IpWhitelist,
            RateLimitQpm = key.RateLimitQpm,
            CreatedAt = key.CreatedAt,
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt
        }).ToList();
    }

    /// <summary>
    /// 删除API Key
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>是否删除成功</returns>
    public async Task<bool> DeleteApiKeyAsync(long id)
    {
        return await _apiKeyRepository.DeleteAsync(id);
    }

    /// <summary>
    /// 禁用API Key
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>是否禁用成功</returns>
    public async Task<bool> DisableApiKeyAsync(long id)
    {
        var apiKey = await _apiKeyRepository.GetByIdAsync(id);
        if (apiKey == null)
        {
            return false;
        }

        apiKey.Status = 0; // 0=禁用
        await _apiKeyRepository.UpdateAsync(apiKey);
        return true;
    }

    /// <summary>
    /// 启用API Key
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>是否启用成功</returns>
    public async Task<bool> EnableApiKeyAsync(long id)
    {
        var apiKey = await _apiKeyRepository.GetByIdAsync(id);
        if (apiKey == null)
        {
            return false;
        }

        apiKey.Status = 1; // 1=正常
        await _apiKeyRepository.UpdateAsync(apiKey);
        return true;
    }

    /// <summary>
    /// 生成API Key
    /// </summary>
    /// <returns>API Key</returns>
    private string GenerateApiKey()
    {
        return $"ak_live_{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 24)}";
    }

    /// <summary>
    /// 生成API Secret
    /// </summary>
    /// <returns>API Secret</returns>
    private string GenerateApiSecret()
    {
        return $"sk_live_{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 24)}";
    }

    /// <summary>
    /// 计算哈希值
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>哈希值</returns>
    private string ComputeHash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }

    /// <summary>
    /// 掩码API Key
    /// </summary>
    /// <param name="apiKeyHash">API Key哈希值</param>
    /// <returns>掩码后的API Key</returns>
    private string MaskApiKey(string apiKeyHash)
    {
        // 显示哈希值的最后8位
        if (apiKeyHash.Length >= 8)
        {
            return $"****{apiKeyHash.Substring(apiKeyHash.Length - 8)}";
        }
        return "****";
    }
}
