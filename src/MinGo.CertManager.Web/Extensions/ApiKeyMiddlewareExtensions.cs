using MinGo.CertManager.Web.Middleware;

namespace MinGo.CertManager.Web.Extensions;

/// <summary>
/// API Key中间件扩展方法
/// </summary>
public static class ApiKeyMiddlewareExtensions
{
    /// <summary>
    /// 使用API Key认证中间件
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器</returns>
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
