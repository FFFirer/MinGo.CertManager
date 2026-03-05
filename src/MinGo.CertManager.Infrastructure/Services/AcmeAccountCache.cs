using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Infrastructure.Services;

public class AcmeAccountCache : IAcmeAccountCache
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AcmeAccountCache> _logger;

    public AcmeAccountCache(ApplicationDbContext dbContext, ILogger<AcmeAccountCache> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AcmeAccount?> GetCachedAccountAsync(string acmeServerUrl, string contact)
    {
        try
        {
            var account = await _dbContext.AcmeAccounts
                .FirstOrDefaultAsync(a => a.AcmeServerUrl == acmeServerUrl && a.Contact == contact);

            if (account != null)
            {
                _logger.LogInformation("找到缓存的ACME账号: AccountId={AccountId}, Contact={Contact}",
                    account.AccountId, account.Contact);
            }
            else
            {
                _logger.LogInformation("未找到缓存的ACME账号: AcmeServerUrl={AcmeServerUrl}, Contact={Contact}",
                    acmeServerUrl, contact);
            }

            return account;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存的ACME账号失败: AcmeServerUrl={AcmeServerUrl}, Contact={Contact}",
                acmeServerUrl, contact);
            return null;
        }
    }

    public async Task CacheAccountAsync(AcmeAccount account)
    {
        try
        {
            account.CreatedAt = DateTime.UtcNow;
            account.LastUsedAt = DateTime.UtcNow;

            _dbContext.AcmeAccounts.Add(account);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("缓存ACME账号成功: AccountId={AccountId}, Contact={Contact}",
                account.AccountId, account.Contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存ACME账号失败: AccountId={AccountId}", account.AccountId);
            throw;
        }
    }

    public async Task UpdateLastUsedAsync(Guid accountId)
    {
        try
        {
            var account = await _dbContext.AcmeAccounts.FindAsync(accountId);
            if (account != null)
            {
                account.LastUsedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("更新ACME账号最后使用时间: AccountId={AccountId}", accountId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新ACME账号最后使用时间失败: AccountId={AccountId}", accountId);
        }
    }
}
