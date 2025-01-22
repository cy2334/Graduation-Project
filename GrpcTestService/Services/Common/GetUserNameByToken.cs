using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Grpc.Core;

namespace GrpcTestService.Services.Common;

public class GetUserProfileNameByToken
{
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

