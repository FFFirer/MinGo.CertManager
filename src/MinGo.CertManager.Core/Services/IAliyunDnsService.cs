namespace MinGo.CertManager.Core.Services;

/// <summary>
/// 阿里云 DNS 服务接口
/// </summary>
public interface IAliyunDnsService
{
    /// <summary>
    /// 清除TXT记录
    /// </summary>
    /// <param name="rootDomain">根域名</param>
    /// <param name="recordName">记录名称</param>
    /// <param name="dnsTxt">DNS TXT值</param>
    Task ClearTxtRecordAsync(string rootDomain, string recordName, string dnsTxt);
    
    /// <summary>
    /// 创建TXT记录
    /// </summary>
    /// <param name="rootDomain">根域名</param>
    /// <param name="recordName">记录名称</param>
    /// <param name="dnsTxt">DNS TXT值</param>
    Task CreateTxtRecordAsync(string rootDomain, string recordName, string dnsTxt);
    
    /// <summary>
    /// 删除TXT记录
    /// </summary>
    /// <param name="rootDomain">根域名</param>
    /// <param name="recordName">记录名称</param>
    /// <param name="dnsTxt">DNS TXT值</param>
    Task DeleteTxtRecordAsync(string rootDomain, string recordName, string dnsTxt);
}
