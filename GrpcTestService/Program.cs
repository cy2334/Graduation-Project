using GrpcTestService.Authentication;
using GrpcTestService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();//添加GRPC反射服务

var rsaKeyHelper = new RsaKeyHelper(privateKeyPath: "private.key", "public.key");
// 添加授权服务
builder.Services.AddAuthorization();
// 添加认证服务
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = false,
            IssuerSigningKey = new RsaSecurityKey(rsaKeyHelper.PublicKey)
        };
    });

var app = builder.Build();
app.MapGrpcReflectionService().AllowAnonymous();
// Configure the HTTP request pipeline.

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<GreeterService>();
app.MapGrpcService<DemoService>();//启用中间件
app.MapGrpcService<TokenService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();