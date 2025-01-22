using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Grpc.Core;
using GrpcTestService.Authentication;
using Microsoft.IdentityModel.Tokens;
using My.GRPC.Demo;

namespace GrpcTestService.Services;

public class TokenService :Token.TokenBase
{
    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        var rsaKeyHelper = new RsaKeyHelper(privateKeyPath:"private.key","public.key");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.UserName),
            new Claim(ClaimTypes.Role, request.UserName == "bsi" ? "admin" : "user"),
            new Claim("userId", "2")
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
        return Task.FromResult(res);
    }
}