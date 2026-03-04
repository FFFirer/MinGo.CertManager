using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MinGo.CertManager.Infrastructure.Services;

namespace MinGo.CertManager.Web.Middleware;

/// <summary>
/// API Key认证中间件
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApiSecretHeaderName = "X-API-Secret";
    private const string ApiTimestampHeaderName = "X-API-Timestamp";
    private const string ApiNonceHeaderName = "X-API-Nonce";
    private const string ApiSignatureHeaderName = "X-API-Signature";
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
    /// 中间件执行方法
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>任务</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 只对外部API路由进行认证
        if (context.Request.Path.StartsWithSegments("/api/external"))
        {
            try
            {
                // 1. 基础参数校验
                if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
                {
                    await ReturnErrorAsync(context, "AUTH_MISSING_API_KEY", "缺少API Key");
                    return;
                }

                if (!context.Request.Headers.TryGetValue(ApiSecretHeaderName, out var providedApiSecret))
                {
                    await ReturnErrorAsync(context, "AUTH_MISSING_API_SECRET", "缺少API Secret");
                    return;
                }

                // 2. 获取API Key服务
                using var scope = _serviceProvider.CreateScope();
                var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

                // 3. 验证API Key
                var isValid = await apiKeyService.ValidateApiKeyAsync(providedApiKey!, providedApiSecret!);
                if (!isValid)
                {
                    await ReturnErrorAsync(context, "AUTH_INVALID_API_KEY", "无效的API Key或Secret");
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
