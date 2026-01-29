using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Infrastructure.Repositories;

public interface IDnsProviderRepository
{
    Task<DnsProvider?> GetByIdAsync(Guid id);
    Task<List<DnsProvider>> GetAllAsync();
    Task<DnsProvider?> GetByProviderTypeAsync(DnsProviderType providerType);
    Task<DnsProvider> AddAsync(DnsProvider provider);
    Task<DnsProvider> UpdateAsync(DnsProvider provider);
    Task DeleteAsync(Guid id);
}

public class DnsProviderRepository : IDnsProviderRepository
{
    private readonly ApplicationDbContext _context;

    public DnsProviderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DnsProvider?> GetByIdAsync(Guid id)
    {
        return await _context.DnsProviders.FindAsync(id);
    }

    public async Task<List<DnsProvider>> GetAllAsync()
    {
        return await _context.DnsProviders.OrderByDescending(d => d.CreatedAt).ToListAsync();
    }

    public async Task<DnsProvider?> GetByProviderTypeAsync(DnsProviderType providerType)
    {
        return await _context.DnsProviders
            .FirstOrDefaultAsync(d => d.ProviderType == providerType);
    }

    public async Task<DnsProvider> AddAsync(DnsProvider provider)
    {
        _context.DnsProviders.Add(provider);
        await _context.SaveChangesAsync();
        return provider;
    }

    public async Task<DnsProvider> UpdateAsync(DnsProvider provider)
    {
        _context.DnsProviders.Update(provider);
        await _context.SaveChangesAsync();
        return provider;
    }

    public async Task DeleteAsync(Guid id)
    {
        var provider = await GetByIdAsync(id);
        if (provider != null)
        {
            _context.DnsProviders.Remove(provider);
            await _context.SaveChangesAsync();
        }
    }
}
