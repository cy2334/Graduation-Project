using Grpc.Core;
using GrpcTestService;

namespace GrpcTestService.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;

    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }
    // [Authorize]
    // public override async Task<ProfileReply> Profile(Empty request, ServerCallContext context)
    // {
    //     var userInfo = context.GetHttpContext().User.Claims.ToArray();
    //     // 提取用户的 Claims 信息
    //     var userIdClaim = userInfo.FirstOrDefault(c => c.Type == "userId")?.Value;
    //     var userNameClaim = userInfo.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
    //     var roleClaim = userInfo.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
    //     // 可以进一步根据需要验证和处理这些信息
    //     var profileReply = new ProfileReply
    //     {
    //         Id = userIdClaim != null ? long.Parse(userIdClaim) : 0,
    //         Name = userNameClaim ?? "Unknown",
    //         Role = roleClaim ?? "Unknown"
    //     };
    //     return profileReply;
    // }
}