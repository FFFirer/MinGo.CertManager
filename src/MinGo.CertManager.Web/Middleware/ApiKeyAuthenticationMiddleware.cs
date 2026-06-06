using System.Text.Json;
using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Web.Middleware;

/// <summary>
/// API Key认证中间件
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string AuthorizationHeaderName = "Authorization";
    private const string ApiKeyScheme = "ApiKey";
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="serviceProvider">服务提供者</param>
    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 从 Authorization 头中提取 ApiKey
    /// </summary>
    private static string? ExtractApiKey(string? authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
            return null;

        const string prefix = ApiKeyScheme + " ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return authorizationHeader[prefix.Length..].Trim();
    }

    /// <summary>
    /// 中间件执行方法
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>任务</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 只对外部API路由进行认证：按 segment 精确匹配 /api/external/{action}
        // 使用 segment 数量校验避免 /api/external2 等路径误匹配
        if (context.Request.Path.StartsWithSegments("/api/external", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // 1. 从 Authorization 头提取 API Key
                var authHeader = context.Request.Headers[AuthorizationHeaderName].FirstOrDefault();
                var apiKey = ExtractApiKey(authHeader);

                if (string.IsNullOrEmpty(apiKey))
                {
                    await ReturnErrorAsync(context, "AUTH_MISSING_API_KEY", "缺少API Key，请使用 Authorization: ApiKey <key> 格式");
                    return;
                }

                // 2. 获取API Key服务
                using var scope = _serviceProvider.CreateScope();
                var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

                // 3. 验证API Key（仅校验 Key 是否存在且有效，Secret 留作后续 HMAC 签名阶段）
                var isValid = await apiKeyService.ValidateApiKeyAsync(apiKey);
                if (!isValid)
                {
                    await ReturnErrorAsync(context, "AUTH_INVALID_API_KEY", "无效的API Key");
                    return;
                }

                // TODO: 实现以下功能
                // 4. IP白名单校验
                // 5. 时间戳和Nonce防重放
                // 6. 签名验证
                // 7. 限流

                // 认证通过，继续处理请求
                await _next(context);
            }
            catch (Exception ex)
            {
                await ReturnErrorAsync(context, "AUTH_ERROR", ex.Message);
                return;
            }
        }
        else
        {
            // 非API路由，直接通过
            await _next(context);
        }
    }

    /// <summary>
    /// 返回错误信息
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="code">错误码</param>
    /// <param name="message">错误消息</param>
    /// <returns>任务</returns>
    private async Task ReturnErrorAsync(HttpContext context, string code, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            code = code,
            message = message,
            request_id = Guid.NewGuid().ToString()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}
