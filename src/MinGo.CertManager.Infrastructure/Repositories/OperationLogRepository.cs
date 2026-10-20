using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Infrastructure.Repositories;

public interface IOperationLogRepository
{
    Task AddAsync(OperationLog log);
    Task<List<OperationLog>> QueryAsync(DateTime? from = null, DateTime? to = null,
        OperationLogAction? action = null, string? targetName = null, int skip = 0, int take = 50);
    Task<int> CountAsync(DateTime? from = null, DateTime? to = null,
        OperationLogAction? action = null, string? targetName = null);
}

public class OperationLogRepository : IOperationLogRepository
{
    private readonly ApplicationDbContext _context;

    public OperationLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OperationLog log)
    {
        _context.OperationLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public Task<List<OperationLog>> QueryAsync(DateTime? from = null, DateTime? to = null,
        OperationLogAction? action = null, string? targetName = null, int skip = 0, int take = 50)
    {
        return BuildQuery(from, to, action, targetName)
            .OrderByDescending(o => o.At)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public Task<int> CountAsync(DateTime? from = null, DateTime? to = null,
        OperationLogAction? action = null, string? targetName = null)
    {
        return BuildQuery(from, to, action, targetName).CountAsync();
    }

    private IQueryable<OperationLog> BuildQuery(DateTime? from, DateTime? to,
        OperationLogAction? action, string? targetName)
    {
        var query = _context.OperationLogs.AsQueryable();
        if (from.HasValue)
        {
            query = query.Where(o => o.At >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(o => o.At <= to.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(o => o.Action == action.Value);
        }

        if (!string.IsNullOrWhiteSpace(targetName))
        {
            var name = targetName.Trim();
            query = query.Where(o => o.TargetName != null && o.TargetName.Contains(name));
        }

        return query;
    }
}
