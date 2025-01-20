using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GrpcTestService.Interceptor;

public class LoggingInterceptor: Grpc.Core.Interceptors.Interceptor
{
    private readonly ILogger<LoggingInterceptor> _logger;
    public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
    {
        _logger = logger;
    }
    // 处理请求和响应
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> next)
    {
        // 记录请求参数
        _logger.LogInformation("Request: {Request}", request.ToString());

        // 调用下一个拦截器/处理方法并获取响应
        var response = await next(request, context);

        // 记录响应信息
        _logger.LogInformation("Response: {Response}", response.ToString());

        return response;
    }
}