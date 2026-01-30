using System;
using System.Threading.Tasks;
using MinGo.CertManager.Core.Entities;

namespace MinGo.CertManager.Infrastructure.Services;

public interface IAcmeAccountCache
{
    Task<AcmeAccount?> GetCachedAccountAsync(string acmeServerUrl, string contact);
    Task CacheAccountAsync(AcmeAccount account);
    Task UpdateLastUsedAsync(Guid accountId);
}
