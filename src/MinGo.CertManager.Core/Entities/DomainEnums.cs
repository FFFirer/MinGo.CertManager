namespace MinGo.CertManager.Core.Entities;

/// <summary>
/// 域名资产状态：Active=有效，Removed=提供商侧已删除（软删保留）
/// </summary>
public enum DomainAssetStatus
{
    Active = 0,
    Removed = 1
}

/// <summary>
/// 域名资产来源：Synced=提供商同步，Manual=手动纳管
/// </summary>
public enum DomainAssetSource
{
    Synced = 0,
    Manual = 1
}
