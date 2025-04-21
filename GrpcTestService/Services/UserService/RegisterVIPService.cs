using Grpc.Core;
using My.GRPC.Demo;

namespace GrpcTestService.Services.UserService;


public class RegisterVIPService : User.UserBase
{
    private readonly AppDbContext _dbContext;

    public RegisterVIPService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override Task<VIPRegisterResponse> VIPRegister(VIPRegisterRequest request, ServerCallContext context)
    {
        bool flag = false;
        foreach (var user in _dbContext.Users)
        {
            if (user.Name == request.Name)
            {
                flag = true;
                break;
            }
        }

        if (flag)
        {
            if (request.VIPpassword == "123456")
            {
                return Task.FromResult( new VIPRegisterResponse
                {
                    Status = "Success",
                });
            }
        }
        else
        {
            throw new RpcException(new Status(StatusCode.NotFound, "用户不存在"));
        }
        
        return base.VIPRegister(request, context);
    }
}