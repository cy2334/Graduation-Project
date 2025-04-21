using Grpc.Core;
using GrpcTestService.Models;
using Microsoft.EntityFrameworkCore.Internal;
using My.GRPC.Demo;
using User = My.GRPC.Demo.User;

namespace GrpcTestService.Services.UserService;

public class RegisterService : User.UserBase
{
    private readonly AppDbContext _dbContext;

    public RegisterService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        if(request.UserName ==null && request.Password == null&&request.Email== null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "输入信息不全"));
        }

        foreach (var user in _dbContext.Users)
        {
            if (user.Name == request.UserName)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "用户以注册"));
            }
            else
            {
                if (user.Email == request.Email)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "邮箱不存在"));
                }
            }
        }
        RegisterResponse response = new RegisterResponse();
        response.Status = "创建成功";
        _dbContext.Users.Add(new Models.User()
        {
            Name = request.UserName,
            Email = request.Email,
            Password = request.Password,
            IsAdmin = false,
            IsVIP = false
        });
        _dbContext.SaveChanges();
        return Task.FromResult(response);
    }
}