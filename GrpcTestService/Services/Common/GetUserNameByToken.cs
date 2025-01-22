using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Grpc.Core;

namespace GrpcTestService.Services.Common;

public class GetUserProfileNameByToken
{
    /// <summary>
    /// 通过Token获取Profile
    /// 从请求的头部获取 Authorization 信息（Bearer <token>）
    /// var token = context.RequestHeaders.FirstOrDefault(header => header.Key == "authorization")?.Value?.Replace("Bearer ", "");
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="RpcException"></exception>
    public Profile getUserProfileNameByToken(string? token)
    {
        // 解析 JWT Token
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
        if (jwtToken == null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid token"));
        }
        // 提取 Claims（用户信息）
        var userIdClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        var userNameClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        var roleClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        // 可以进一步根据需要验证和处理这些信息
        var profile = new Profile(
            userIdClaim != null ? long.Parse(userIdClaim) : 0,
            userNameClaim ?? "Unknown",
            roleClaim ?? "Unknown"
            );
        return profile;
    }
}
/// <summary>
/// 用户Profile
/// </summary>
public class Profile
{ 
    // Constructor to initialize Profile
    public Profile(long id, string name, string role)
    {
        Id = id;
        Name = name;
        Role = role;
    }
     long Id { get;  set; }
     string Name { get; set; }
     string Role { get; set; }
}

