using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Infrastructure.Repositories;

/// <summary>
/// API Key仓库实现
/// </summary>
public class ApiKeyRepository : IApiKeyRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public ApiKeyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 创建API Key
    /// </summary>
    /// <param name="apiKey">API Key实体</param>
    /// <returns>创建的API Key</returns>
    public async Task<ApiKey> CreateAsync(ApiKey apiKey)
    {
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();
        return apiKey;
    }

    /// <summary>
    /// 根据ID获取API Key
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>API Key实体</returns>
    public async Task<ApiKey?> GetByIdAsync(long id)
    {
        return await _context.ApiKeys.FindAsync(id);
    }

    /// <summary>
    /// 根据API Key哈希获取API Key
    /// </summary>
    /// <param name="apiKeyHash">API Key哈希值</param>
    /// <returns>API Key实体</returns>
    public async Task<ApiKey?> GetByApiKeyHashAsync(string apiKeyHash)
    {
        return await _context.ApiKeys.FirstOrDefaultAsync(k => k.ApiKeyHash == apiKeyHash);
    }

    /// <summary>
    /// 获取所有API Key
    /// </summary>
    /// <returns>API Key列表</returns>
    public async Task<List<ApiKey>> GetAllAsync()
    {
        return await _context.ApiKeys.ToListAsync();
    }

    /// <summary>
    /// 更新API Key
    /// </summary>
    /// <param name="apiKey">API Key实体</param>
    /// <returns>更新后的API Key</returns>
    public async Task<ApiKey> UpdateAsync(ApiKey apiKey)
    {
        apiKey.UpdatedAt = DateTime.UtcNow;
        _context.ApiKeys.Update(apiKey);
        await _context.SaveChangesAsync();
        return apiKey;
    }

    /// <summary>
    /// 删除API Key
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>是否删除成功</returns>
    public async Task<bool> DeleteAsync(long id)
    {
        var apiKey = await _context.ApiKeys.FindAsync(id);
        if (apiKey == null)
        {
            return false;
        }

        _context.ApiKeys.Remove(apiKey);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 更新API Key的最后使用时间
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <returns>是否更新成功</returns>
    public async Task<bool> UpdateLastUsedAsync(long id)
    {
        var apiKey = await _context.ApiKeys.FindAsync(id);
        if (apiKey == null)
        {
            return false;
        }

        apiKey.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
