using System;
using System.Threading.Tasks;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Repositories;

namespace MinGo.CertManager.Infrastructure.Services;

public interface IDnsValidationService
{
    Task CreateTxtRecordAsync(string domain, string recordName, string value);
    Task DeleteTxtRecordAsync(string domain, string recordName, string value);
}

public class AliyunDnsValidationService : IDnsValidationService
{
    private readonly IDnsProviderRepository _dnsProviderRepository;

    public AliyunDnsValidationService(IDnsProviderRepository dnsProviderRepository)
    {
        _dnsProviderRepository = dnsProviderRepository;
    }

    public async Task CreateTxtRecordAsync(string domain, string recordName, string value)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteTxtRecordAsync(string domain, string recordName, string value)
    {
        await Task.CompletedTask;
    }
}
