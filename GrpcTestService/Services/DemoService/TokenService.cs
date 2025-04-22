using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Google.Protobuf.Collections;
using Grpc.Core;
using GrpcTestService.Authentication;
using GrpcTestService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using My.GRPC.Demo;
using User = My.GRPC.Demo.User;

namespace GrpcTestService.Services;

public class TokenService :User.UserBase
{
    private readonly AppDbContext _dbContext;

    public TokenService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Name == request.Username);
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "用户不存在"));
        }
        // 3. 验证密码（示例使用简单验证，实际应使用密码哈希）
        if (user.Password != request.Password) // 注意：实际项目请使用密码哈希比较！
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "密码错误"));
        }

        var rsaKeyHelper = new RsaKeyHelper(privateKeyPath:"private.key","public.key");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "admin" : "user"),
            new Claim("userId", user.Id.ToString())
        };
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsaKeyHelper.PrivateKey), SecurityAlgorithms.RsaSha256)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        var res = new LoginResponse();
        res.Token = tokenHandler.WriteToken(token);
        
        return await Task.FromResult(res);
    }
    public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        if(request.Username ==null && request.Password == null&&request.Email== null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "输入信息不全"));
        }

        foreach (var user in _dbContext.Users)
        {
            if (user.Name == request.Username)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "用户以注册"));
            }
            else
            {
                if (user.Email == request.Email)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "邮箱重复"));
                }
            }
        }
        RegisterResponse response = new RegisterResponse();
        response.Status = "Success";
        _dbContext.Users.Add(new Models.User()
        {
            Name = request.Username,
            Email = request.Email,
            Password = request.Password,
            IsAdmin = false,
            IsVIP = false
        });
        _dbContext.SaveChanges();
        return Task.FromResult(response);
    }
    public override Task<CalculateAmountResponse> CalculateAmount(CalculateAmountRequest request, ServerCallContext context)
    {
        var VIPinfos = _dbContext.VIPInfomations;
        RepeatedField<VIPInfo> vipInfos = new RepeatedField<VIPInfo>();

        foreach (var VIPinfo in VIPinfos)
        {
            VIPinfo.recharge -= VIPinfo.Dailyspending;

            var vipInfo = new VIPInfo
            {
                Recharge = VIPinfo.recharge,
                VIPname = _dbContext.Users.FirstOrDefault(v => v.Id == VIPinfo.UserId)?.Name ?? ""
            };

            vipInfos.Add(vipInfo);
        }

        _dbContext.SaveChanges(); // 如果你希望保存每日扣费的话，别忘了

        var response = new CalculateAmountResponse();
        response.Vipinfos.AddRange(vipInfos); // 注意：不能用赋值，只能 AddRange

        return Task.FromResult(response);
    }
    public override Task<VIPRegisterResponse> VIPRegister(VIPRegisterRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "名称不能为空"));
        }

        if (request.Recharge <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "充值金额必须大于0"));
        }

        var user = _dbContext.Users.FirstOrDefault(u => u.Name == request.Name);
        if (user == null)
        {
            return Task.FromResult(new VIPRegisterResponse
            {
                Status = "找不到用户，创建VIP失败"
            });
        }

        var vip = new VIPInfomation
        {
            UserId = user.Id,
            recharge = request.Recharge
        };

        _dbContext.VIPInfomations.Add(vip);
        user.IsVIP = true;
        _dbContext.SaveChanges();

        return Task.FromResult(new VIPRegisterResponse
        {
            Status = "创建VIP成功"
        });
    }
    public override Task<RegisterAdminResponse> RegisterAdmin(RegisterAdminRequest request, ServerCallContext context)
    {
        var user = _dbContext.Users.FirstOrDefault(v => v.Name == request.Username);
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound,"用户不存在"));
        }

        if (user.IsAdmin)
        {
            throw new RpcException(new Status(StatusCode.NotFound,"用户以为管理员"));  
        }

        if (request.Adminpassword == "123456")
        {
            user.IsAdmin = true;
        }
        _dbContext.SaveChanges();
        return Task.FromResult(new RegisterAdminResponse()
        {
            Status = user.Name + "以为管理员"
        });
    }
}