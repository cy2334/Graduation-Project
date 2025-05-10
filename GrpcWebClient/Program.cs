using Grpc.Core;
using GrpcTestService;
using GrpcWebClient.Model;
using Microsoft.AspNetCore.Mvc;
using My.GRPC.Demo;

var builder = WebApplication.CreateBuilder(args);

// ✅ 添加 MVC 控制器支持
builder.Services.AddControllers();

// ✅ 添加 CORS 策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ✅ 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ 添加身份验证（如果你用到 JWT 或其他认证机制）
builder.Services.AddAuthorization();

// ✅ 添加 gRPC 客户端服务
builder.Services.AddGrpcClient<Warehouse.WarehouseClient>(o =>
{
    o.Address = new Uri("http://localhost:5069"); // 修改为你的 gRPC 真实地址
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
});

builder.Services.AddGrpcClient<User.UserClient>(o =>
{
    o.Address = new Uri("http://localhost:5069"); // 同样改成你 User 服务的 gRPC 地址
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
});
builder.Services.AddGrpcClient<Admin.AdminClient>(o =>
{
    o.Address = new Uri("http://localhost:5069"); // 同样改成你 User 服务的 gRPC 地址
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
});
var app = builder.Build();

// ✅ 中间件配置
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAllOrigins");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers(); // 如果你写了传统控制器 API

// ✅ 最小API写法的 gRPC 映射
app.MapPost("/api/login", async ([FromServices] User.UserClient client, [FromBody] LoginRequestDto loginDto) =>
{
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
}).WithName("Login").WithOpenApi();

app.MapPost("/api/register", async ([FromServices] User.UserClient client, [FromBody] RegisterRequestDto registerDto) =>
{
    var res = await client.RegisterAsync(new RegisterRequest
    {
        Username = registerDto.Username,
        Email = registerDto.Email,
        Password = registerDto.Password
    });

    return res.Status == "Success"
        ? Results.Ok(new { message = "注册成功" })
        : Results.BadRequest(new { message = "注册失败" });
}).WithName("Register").WithOpenApi();

app.MapGet("/api/warehousesdisplay", async (
    [FromServices] Warehouse.WarehouseClient client,
    [FromQuery] int pageIndex = 1,
    [FromQuery] int pageSize = 10) =>
{
    var grpcResponse = await client.WarehouseDisplayAsync(new WarehouseDisplayRequest
    {
        Pageindex = pageIndex,
        Pagesize = pageSize
    });

    return Results.Ok(new
    {
        warehouseInfos = grpcResponse.Warehouseinfos,
        pageSize = grpcResponse.Pagesize,
        pageIndex = grpcResponse.Pageindex,
        total = grpcResponse.Total
    });
}).WithName("GetWarehouses").WithOpenApi();

app.MapPost("/api/warehouses/register", async (
    [FromServices] Warehouse.WarehouseClient client,
    [FromBody] WarehouseRequest request) =>
{
    var grpcResponse = await client.WarehouseRegisterAsync(request);

    return Results.Ok(new
    {
        status = grpcResponse.Status
    });
}).WithName("RegisterWarehouse").WithOpenApi();
app.MapPost("/api/warehouses/warehousing", async (
        [FromServices] Warehouse.WarehouseClient client,
        [FromBody] WarehousingofgoodsRequest request) =>
    {
        var grpcResponse = await client.WarehousingofgoodsAsync(request);

        return Results.Ok(new
        {
            remainingVolume = grpcResponse.Remainingvolumeofthewarehouse,
            costPerDay = grpcResponse.CostofDay
        });
    })
    .WithName("WarehousingOfGoods")
    .WithOpenApi();
app.MapPost("/api/user/vipregister", async (
        [FromServices] User.UserClient client, // 使用 UserClient，而不是 VIPClient
        [FromBody] VIPRegisterRequest request) =>
    {
        var grpcResponse = await client.VIPRegisterAsync(request);

        return Results.Ok(new
        {
            status = grpcResponse.Status
        });
    })
    .WithName("VIPRegister")
    .WithOpenApi();

    app.MapPost("/api/warehouses/warehouse-distance", async (
            [FromServices] Warehouse.WarehouseClient client,
            [FromBody] WarehouseDistanceRequest request) =>
        {
            var grpcResponse = await client.WarehouseDistanceAsync(request);
    
            return Results.Ok(new
            {
                status = grpcResponse.Status
            });
        })
        .WithName("WarehouseDistance")
        .WithOpenApi();
    app.MapPost("/api/warehouses/warehouse-transport", async (
            [FromServices] Warehouse.WarehouseClient client,
            [FromBody] WarehouseTransportRequest request) =>
        {
            var grpcResponse = await client.WarehouseTransportAsync(request);

            return Results.Ok(new
            {
                distance = grpcResponse.Distance,
                cost = grpcResponse.Cost
            });
        })
        .WithName("WarehouseTransport")
        .WithOpenApi();
    app.MapGet("/api/user/getuserinfo", async (
            [FromServices] User.UserClient client, // 使用 UserClient 处理 gRPC 请求
            [FromQuery] int userID) => // 通过查询参数获取 userID
        {
            // 创建 GetUserInfoRequest 请求对象
            var request = new GetUserInfoRequest { UserID = userID };

            // 调用 gRPC 客户端的 GetUserInfo 方法，传递请求数据
            var grpcResponse = await client.GetUserInfoAsync(request);

            // 返回包含 gRPC 响应数据的结果
            return Results.Ok(new
            {
                cost = grpcResponse.Cost,
                costOfDay = grpcResponse.CostofDay,
                userStatus = grpcResponse.Userstatus
            });
        })
        .WithName("GetUserInfo") // 定义路由名称
        .WithOpenApi(); // 为该 API 生成 OpenAPI 文档
    app.MapPost("/api/warehouses/outbound", async (
            [FromServices] Warehouse.WarehouseClient client,
            [FromBody] GoodsOutboundRequest request) =>
        {
            var grpcResponse = await client.GoodsOutboundAsync(request);

            return Results.Ok(new
            {
                remainingGoods = grpcResponse.RemainingGoods,
                status = grpcResponse.Status
            });
        })
        .WithName("GoodsOutbound")
        .WithOpenApi();
    app.MapGet("/api/user/orders", async (
            [FromServices] User.UserClient client,   // 注入 gRPC 客户端
            [FromQuery] int userId,                  // 查询参数：用户 ID
            [FromQuery] int pageIndex = 1,           // 查询参数：页码，默认 1
            [FromQuery] int pageSize = 10            // 查询参数：页大小，默认 10
        ) =>
        {
            // 1. 构造 gRPC 请求
            var grpcRequest = new GetOrdersRequest
            {
                UserId    = userId,
                PageIndex = pageIndex,
                PageSize  = pageSize
            };

            // 2. 调用 gRPC 服务
            var grpcResponse = await client.GetOrdersAsync(grpcRequest);

            // 3. 将 gRPC 响应映射为 HTTP JSON
            return Results.Ok(new
            {
                orders    = grpcResponse.Orders.Select(o => new
                {
                    warehouseAName     = o.WarehouseAName,
                    warehouseBName     = o.WarehouseBName,
                    price              = o.Price,
                    driverName         = o.DriverName,
                    driverPhone        = o.DriverPhone,
                    licensePlateNumber = o.LicensePlateNumber,
                    orderDate          = o.OrderDate.ToDateTime() // 转回 DateTime
                }),
                pageIndex = grpcResponse.PageIndex,
                pageSize  = grpcResponse.PageSize,
                total     = grpcResponse.Total
            });
        })
        .WithName("GetOrders")
        .WithOpenApi();

app.MapGet("/api/admin/orderoverview", async (
        [FromServices] Admin.AdminClient client, // 使用 AdminClient 调用 gRPC 服务
        [FromQuery] string? username,
        [FromQuery] int pageIndex,
        [FromQuery] int pageSize) =>
    {
        var request = new OrderOverviewRequest
        {
            Username = username ?? "",
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        var grpcResponse =  client.OrderOverview(request);

        return Results.Ok(new
        {
            pageIndex = grpcResponse.PageIndex,
            pageSize = grpcResponse.PageSize,
            total = grpcResponse.Total,
            orders = grpcResponse.Orders.Select(o => new
            {
                o.WarehouseAName,
                o.WarehouseBName,
                o.Price,
                o.DriverName,
                o.DriverPhone,
                o.LicensePlateNumber,
                OrderDate = o.OrderDate.ToDateTime()
            })
        });
    })
    .WithName("GetOrderOverview")
    .WithOpenApi();
app.MapGet("/api/admin/overdueuserlist", async (
        [FromServices] Admin.AdminClient client,
        [FromQuery] int pageIndex,
        [FromQuery] int pageSize,
        [FromQuery] string? userName) =>
    {
        var request = new OverdueUserListRequest
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            UserName = userName ?? ""
        };

        var grpcResponse = await client.OverdueUserListAsync(request);

        return Results.Ok(new
        {
            pageIndex = grpcResponse.PageIndex,
            pageSize = grpcResponse.PageSize,
            total = grpcResponse.Total,
            users = grpcResponse.OverdueUser.Select(u => new
            {
                u.UserName,
                OverdueAmount = u.OverdueAmount,
                u.Email
            })
        });
    })
    .WithName("GetOverdueUserList")
    .WithOpenApi();
app.MapGet("/api/admin/dailysettlement", async (
        [FromServices] Admin.AdminClient client,
        [FromQuery] int pageIndex,
        [FromQuery] int pageSize) =>
    {
        var request = new DailySettlementRequest
        {
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        var grpcResponse = await client.DailySettlementAsync(request);

        return Results.Ok(new
        {
            pageIndex = grpcResponse.PageIndex,
            pageSize = grpcResponse.PageSize,
            total = grpcResponse.Total,
            overdueUsers = grpcResponse.OverdueUser.Select(u => new
            {
                u.UserName,
                OverdueAmount = u.OverdueAmount,
                u.Email
            })
        });
    })
    .WithName("GetDailySettlement")
    .WithOpenApi();
app.MapPost("/api/admin/register", async (
        [FromServices] Admin.AdminClient client,
        [FromBody] AdminRegistrationRequest request) =>
    {
        try
        {
            var grpcResponse = await client.AdminRegistrationAsync(request);
            return Results.Ok(new
            {
                status = grpcResponse.Status // ✅ 正确访问 Status 属性
            });
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"gRPC Error: {ex.Status.Detail}");
            return Results.Problem(ex.Status.Detail);
        }
    })
    .WithName("AdminRegister")
    .WithOpenApi();
app.Run();
