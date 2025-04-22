using GrpcTestService;
using GrpcWebClient.Model;
using Microsoft.AspNetCore.Mvc;
using My.GRPC.Demo;

var builder = WebApplication.CreateBuilder(args);

// ✅ 添加 MVC 控制器支持（否则 MapControllers() 会报错）
builder.Services.AddControllers();

// 添加 CORS 规则
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy => policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// ✅ 添加身份验证和授权
builder.Services.AddAuthorization();

// 添加 gRPC 客户端
builder.Services.AddGrpcClient<User.UserClient>(options =>
{
    options.Address = new Uri("http://localhost:5069"); 
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// 配置 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 允许 CORS（必须在 UseRouting 之前）
app.UseCors("AllowAllOrigins");

// 配置 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();

// ✅ 确保 AddAuthorization() 先注册，否则 UseAuthorization() 会报错
app.UseAuthorization();


app.MapPost("/api/login", async ([FromServices] User.UserClient client, [FromBody] LoginRequestDto loginDto) =>
    {
        // 调用 gRPC 的 Login 方法
        var res = await client.LoginAsync(new LoginRequest
        {
            Username = loginDto.UserName,
            Password = loginDto.Password
        });

        return Results.Ok(new
        {
            message = "登录成功",
            token = res.Token
        });
    })
    .WithName("Login")
    .WithOpenApi(); // Swagger 中显示
app.MapPost("/api/register", async ([FromServices] User.UserClient client, [FromBody] RegisterRequestDto registerDto) =>
    {
        // 调用 gRPC 的 Register 方法
        var res = await client.RegisterAsync(new RegisterRequest
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            Password = registerDto.Password
        });

        return res.Status == "Success" 
            ? Results.Ok(new { message = "注册成功" }) 
            : Results.BadRequest(new { message = "注册失败" });
    })
    .WithName("Register")
    .WithOpenApi(); // Swagger 中显示


// ✅ 添加控制器支持，否则会报错
app.MapControllers();

app.Run();


