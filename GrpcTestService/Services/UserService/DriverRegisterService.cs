using Grpc.Core;
using My.GRPC.Demo;

namespace GrpcTestService.Services.UserService;

public class DriverRegisterService:Driver.DriverBase
{
    private readonly AppDbContext _dbContext;

    public DriverRegisterService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override Task<DriverRegisterResponse> DriverRegister(DriverRegisterRequest request, ServerCallContext context)
    {
        if (request.UserName == null || request.UserName.Length == 0 && request.PhoneNumber == null)
        { 
            throw new RpcException(new Status(StatusCode.NotFound, "输入信息不全"));
        }

        foreach (var user in _dbContext.Users)
        {
            if (user.Name == request.UserName)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "用户名称重复"));
            }
        }
        _dbContext.Drivers.Add(new Models.Driver()
        {
          Name = request.UserName,
          PhoneNumber = request.PhoneNumber,
        });
        
        _dbContext.SaveChanges();
        return Task.FromResult(new DriverRegisterResponse
        {
            Status = "注册成功"
        });
    }
}