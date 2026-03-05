using System;
using System.Threading.Tasks;
using MinGo.CertManager.Core.Entities;

namespace MinGo.CertManager.Core.Services;

/// <summary>
/// ACME账户缓存接口
/// </summary>
public interface IAcmeAccountCache
{
    /// <summary>
    /// 获取缓存的ACME账户
    /// </summary>
    /// <param name="acmeServerUrl">ACME服务器URL</param>
    /// <param name="contact">联系方式</param>
    /// <returns>ACME账户，如果不存在则返回null</returns>
    Task<AcmeAccount?> GetCachedAccountAsync(string acmeServerUrl, string contact);
    
    /// <summary>
    /// 缓存ACME账户
    /// </summary>
    /// <param name="account">ACME账户</param>
    Task CacheAccountAsync(AcmeAccount account);
    
    /// <summary>
    /// 更新最后使用时间
    /// </summary>
    /// <param name="accountId">账户ID</param>
    Task UpdateLastUsedAsync(Guid accountId);
}
