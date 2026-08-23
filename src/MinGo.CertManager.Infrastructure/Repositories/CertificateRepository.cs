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
            .OrderByDescending(c => c.CreatedAt)
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
