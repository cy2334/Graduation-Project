using Calzolari.Grpc.AspNetCore.Validation;
using FluentValidation;
using GrpcTestService.Authentication;
using GrpcTestService.Interceptor;
using GrpcTestService.Services;
using GrpcTestService.Services.UserService;
using GrpcTestService.Valid;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 注册日志记录拦截器
builder.Services.AddSingleton<LoggingInterceptor>();
// Add services to the container.
builder.Services.AddGrpc(options =>
{
    options.EnableMessageValidation();
    options.Interceptors.Add<LoggingInterceptor>(); // 将拦截器加入管道
});
//添加GRPC反射服务
builder.Services.AddGrpcReflection();
// 注入上下文
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValid>();
builder.Services.AddGrpcValidation();

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
//CreateHostBuilder(args).Build();

var app = builder.Build();
app.MapGrpcReflectionService().AllowAnonymous();
// Configure the HTTP request pipeline.

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<GreeterService>();
app.MapGrpcService<DemoService>();//启用中间件
app.MapGrpcService<TokenService>();
app.MapGrpcService<DriverRegisterService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureLogging((context, logging) =>
        {
            // 配置日志记录到控制台
            logging.AddConsole();
        })
        .ConfigureServices((hostContext, services) =>
        {
            // 注册 gRPC 服务和拦截器
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<LoggingInterceptor>();  // 添加拦截器
            });

            services.AddSingleton<LoggingInterceptor>();  // 注册拦截器
        });