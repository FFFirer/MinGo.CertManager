using Microsoft.AspNetCore.Components;

namespace MinGo.CertManager.Web.Components;

/// <summary>
/// 列定义数据模型，由 DataTableColumn 组件填充并注册到 DataTable。
/// </summary>
public sealed class DataTableColumnDef<TItem>
{
    public string Header { get; set; } = string.Empty;
    public RenderFragment<TItem> CellTemplate { get; set; } = _ => builder => { };
    public bool VisibleOnMobile { get; set; } = true;
    public bool IsIdentifying { get; set; }
    public string? CssClass { get; set; }
    public string? Width { get; set; }
}
