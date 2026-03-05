using MinGo.CertManager.Core.Entities;

namespace MinGo.CertManager.Infrastructure.Repositories;

/// <summary>
/// API Key仓库接口
/// </summary>
public interface IApiKeyRepository
{
    /// <summary>
    /// 创建API Key
    /// </summary>
    /// <param name="apiKey">API Key实体</param>
    /// <returns>创建的API Key</returns>
    Task<ApiKey> CreateAsync(ApiKey apiKey);
    
    /// <summary>
    /// 根据ID获取API Key
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>API Key实体</returns>
    Task<ApiKey?> GetByIdAsync(long id);
    
    /// <summary>
    /// 根据API Key哈希获取API Key
    /// </summary>
    /// <param name="apiKeyHash">API Key哈希值</param>
    /// <returns>API Key实体</returns>
    Task<ApiKey?> GetByApiKeyHashAsync(string apiKeyHash);
    
    /// <summary>
    /// 获取所有API Key
    /// </summary>
    /// <returns>API Key列表</returns>
    Task<List<ApiKey>> GetAllAsync();
    
    /// <summary>
    /// 更新API Key
    /// </summary>
    /// <param name="apiKey">API Key实体</param>
    /// <returns>更新后的API Key</returns>
    Task<ApiKey> UpdateAsync(ApiKey apiKey);
    
    /// <summary>
    /// 删除API Key
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(long id);
    
    /// <summary>
    /// 更新API Key的最后使用时间
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateLastUsedAsync(long id);
}
