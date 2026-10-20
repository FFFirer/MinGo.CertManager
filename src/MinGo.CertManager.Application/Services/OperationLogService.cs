using System.Diagnostics;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Repositories;

namespace MinGo.CertManager.Application.Services;

/// <summary>
/// 域名 / DNS 操作流水写入与查询
/// </summary>
public interface IOperationLogService
{
    Task LogAsync(OperationLogAction action, OperationLogResult result, DnsProviderType providerType,
        string targetType, string? targetId, string? targetName, string message,
        string? payload = null, string? actor = null);

    Task<(List<OperationLog> Items, int Total)> QueryAsync(
        DateTime? from = null, DateTime? to = null,
        OperationLogAction? action = null, string? targetName = null,
        int skip = 0, int take = 50);
}

public class OperationLogService : IOperationLogService
{
    /// <summary>Message 最大长度，防止异常串撑爆字段</summary>
    private const int MaxMessageLength = 2000;

    private readonly IOperationLogRepository _repository;

    public OperationLogService(IOperationLogRepository repository)
    {
        _repository = repository;
    }

    public async Task LogAsync(OperationLogAction action, OperationLogResult result, DnsProviderType providerType,
        string targetType, string? targetId, string? targetName, string message,
        string? payload = null, string? actor = null)
    {
        var log = new OperationLog
        {
            Id = Guid.NewGuid(),
            At = DateTime.UtcNow,
            Actor = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim(),
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            TargetName = targetName,
            ProviderType = providerType,
            Result = result,
            Message = Truncate(Sanitize(message), MaxMessageLength),
            TraceId = Activity.Current?.TraceId.ToString(),
            Payload = payload
        };

        await _repository.AddAsync(log);
    }

    public Task<(List<OperationLog> Items, int Total)> QueryAsync(
        DateTime? from = null, DateTime? to = null,
        OperationLogAction? action = null, string? targetName = null,
        int skip = 0, int take = 50)
    {
        return QueryInternalAsync(from, to, action, targetName, skip, take);
    }

    private async Task<(List<OperationLog> Items, int Total)> QueryInternalAsync(
        DateTime? from, DateTime? to, OperationLogAction? action, string? targetName, int skip, int take)
    {
        var items = await _repository.QueryAsync(from, to, action, targetName, skip, take);
        var total = await _repository.CountAsync(from, to, action, targetName);
        return (items, total);
    }

    /// <summary>剔除常见凭据样式子串，避免 AccessKey/Secret 入库</summary>
    internal static string Sanitize(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        // 过长 Base64/Hex 疑似密钥：按空白切词后过滤
        var parts = message.Split(' ');
        for (var i = 0; i < parts.Length; i++)
        {
            var token = parts[i];
            if (token.Length >= 24 && IsOpaqueSecret(token))
            {
                parts[i] = "***";
            }
        }

        return string.Join(' ', parts);
    }

    private static bool IsOpaqueSecret(string token)
    {
        var isHex = true;
        var isBase64 = true;
        foreach (var ch in token)
        {
            if (!Uri.IsHexDigit(ch))
            {
                isHex = false;
            }

            if (!(char.IsLetterOrDigit(ch) || ch is '+' or '/' or '=' or '-' or '_'))
            {
                isBase64 = false;
            }

            if (!isHex && !isBase64)
            {
                return false;
            }
        }

        return isHex || isBase64;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
