using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Infrastructure.Repositories;

public interface ICertificateRepository
{
    Task<Certificate?> GetByIdAsync(Guid id);
    Task<List<Certificate>> GetAllAsync();
    Task<List<Certificate>> GetByStatusAsync(CertificateStatus status);
    Task<List<Certificate>> SearchByDomainAsync(string domain);
    Task<Certificate?> GetLatestValidCertificateByDomainAsync(string domain);
    Task<bool> HasPendingCertificateAsync(string domain, bool isWildcard);
    Task<bool> HasPendingCertificateAsync(string domain, bool isWildcard, bool useStaging);
    Task<List<Certificate>> GetRenewalCandidatesAsync(DateTime expiringBeforeUtc);
    Task<List<Certificate>> GetRecoverablePendingAsync(DateTime? staleBeforeUtc = null);
    Task<Certificate> AddAsync(Certificate certificate);
    Task<Certificate> UpdateAsync(Certificate certificate);
    Task DeleteAsync(Guid id);
}

public class CertificateRepository : ICertificateRepository
{
    private readonly ApplicationDbContext _context;

    public CertificateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Certificate?> GetByIdAsync(Guid id)
    {
        return await _context.Certificates.FindAsync(id);
    }

    public async Task<List<Certificate>> GetAllAsync()
    {
        return await _context.Certificates.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<List<Certificate>> GetByStatusAsync(CertificateStatus status)
    {
        return await _context.Certificates
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Certificate>> SearchByDomainAsync(string domain)
    {
        return await _context.Certificates
            .Where(c => c.Domain.Contains(domain))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Certificate?> GetLatestValidCertificateByDomainAsync(string domain)
    {
        return await _context.Certificates
            .Where(c => c.Domain == domain && c.Status == CertificateStatus.Active)
            .OrderByDescending(c => c.ExpiresAt)
            .ThenByDescending(c => c.IssuedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasPendingCertificateAsync(string domain, bool isWildcard)
    {
        return await _context.Certificates
            .AnyAsync(c => c.Domain == domain &&
                           c.IsWildcard == isWildcard &&
                           c.AcmeStatus >= AcmeProcessStatus.Initializing &&
                           c.AcmeStatus < AcmeProcessStatus.Completed);
    }

    public async Task<bool> HasPendingCertificateAsync(string domain, bool isWildcard, bool useStaging)
    {
        return await _context.Certificates
            .AnyAsync(c => c.Domain == domain &&
                           c.IsWildcard == isWildcard &&
                           c.UseStaging == useStaging &&
                           c.AcmeStatus >= AcmeProcessStatus.Initializing &&
                           c.AcmeStatus < AcmeProcessStatus.Completed);
    }

    public async Task<List<Certificate>> GetRenewalCandidatesAsync(DateTime expiringBeforeUtc)
    {
        // Active 临期，且为同标识下 ExpiresAt 最晚的在役记录，且同标识无 Pending 在途
        return await _context.Certificates
            .Where(c => c.Status == CertificateStatus.Active
                        && c.ExpiresAt <= expiringBeforeUtc
                        && !_context.Certificates.Any(later =>
                            later.Domain == c.Domain &&
                            later.IsWildcard == c.IsWildcard &&
                            later.UseStaging == c.UseStaging &&
                            later.Status == CertificateStatus.Active &&
                            later.ExpiresAt > c.ExpiresAt)
                        && !_context.Certificates.Any(pending =>
                            pending.Domain == c.Domain &&
                            pending.IsWildcard == c.IsWildcard &&
                            pending.UseStaging == c.UseStaging &&
                            pending.AcmeStatus >= AcmeProcessStatus.Initializing &&
                            pending.AcmeStatus < AcmeProcessStatus.Completed))
            .OrderBy(c => c.ExpiresAt)
            .ToListAsync();
    }

    public async Task<List<Certificate>> GetRecoverablePendingAsync(DateTime? staleBeforeUtc = null)
    {
        // Status=Pending：申请/续期新建记录尚未落到 Active/Failed。
        // staleBeforeUtc 非空时只取长时间未更新的陈旧记录，避免与在途作业并发。
        var query = _context.Certificates
            .Where(c => c.Status == CertificateStatus.Pending);

        if (staleBeforeUtc.HasValue)
        {
            var cutoff = staleBeforeUtc.Value;
            query = query.Where(c => c.UpdatedAt <= cutoff);
        }

        return await query
            .OrderBy(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Certificate> AddAsync(Certificate certificate)
    {
        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();
        return certificate;
    }

    public async Task<Certificate> UpdateAsync(Certificate certificate)
    {
        _context.Certificates.Update(certificate);
        await _context.SaveChangesAsync();
        return certificate;
    }

    public async Task DeleteAsync(Guid id)
    {
        var certificate = await GetByIdAsync(id);
        if (certificate != null)
        {
            _context.Certificates.Remove(certificate);
            await _context.SaveChangesAsync();
        }
    }
}
