using GrpcTestService;
using Microsoft.AspNetCore.Mvc;

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
builder.Services.AddGrpcClient<Greeter.GreeterClient>(options =>
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

// 定义天气数据
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// 异步 gRPC 请求
app.MapGet("/weatherforecast", async ([FromServices] Greeter.GreeterClient client, HttpContext context) =>
    {
        // 从请求头中获取 JWT 令牌
        var token = context.Request.Headers["Authorization"].ToString();

        // 创建 HttpClient 实例
        using var httpClient = new HttpClient();

        // 设置请求头（传递 JWT 令牌）
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));

        // 调用后端服务的 HTTP 端点
        var response = await httpClient.GetAsync("http://localhost:5069/api/user/profile");
        if (!response.IsSuccessStatusCode)
        {
            return Results.BadRequest("Failed to fetch user profile.");
        }

        // 解析响应内容
        var profile = await response.Content.ReadFromJsonAsync<Profile>();

        // 调用 gRPC 服务，传递用户信息
        var res = await client.SayHelloAsync(new HelloRequest { Name = profile.Name });

        // 生成天气数据
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            )).ToArray();

        // 返回结果
        return Results.Json(new { message = res.Message, forecast, user = profile.Name });
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

// ✅ 添加控制器支持，否则会报错
app.MapControllers();

app.Run();

// 定义 `WeatherForecast` 记录类型
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
public class Profile
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
}
