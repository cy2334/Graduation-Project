using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Grpc.Core;
using GrpcTestService.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using My.GRPC.Demo;

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
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Name == request.UserName);
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
            new Claim(JwtRegisteredClaimNames.Sub, request.UserName),
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