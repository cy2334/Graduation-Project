using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
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
                Status = "找不到用户，创建/充值VIP失败"
            });
        }

        var vip = _dbContext.VIPInfomations.FirstOrDefault(v => v.UserId == user.Id);
        if (vip == null)
        {
            // 没有VIP信息，新建
            vip = new VIPInfomation
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
        else
        {
            // 已是VIP，充值
            vip.recharge += request.Recharge;
            _dbContext.SaveChanges();

            return Task.FromResult(new VIPRegisterResponse
            {
                Status = "充值成功，当前余额：" + vip.recharge
            });
        }
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

    public override Task<GetUserInfoResponse> GetUserInfo(GetUserInfoRequest request, ServerCallContext context)
    {
        var response = new GetUserInfoResponse();
        var user = _dbContext.Users.FirstOrDefault(v => v.Id == request.UserID);
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound,"用户不存在"));  
        }
        var vipinfo = _dbContext.VIPInfomations.FirstOrDefault(v => v.UserId == user.Id);
        if (vipinfo == null)
        {
            response = new GetUserInfoResponse()
            {
                Cost = 0,
                CostofDay = 0,
                Userstatus = "用户不为vip"
            };
        }
        else
        {
            response = new GetUserInfoResponse()
            {
                Cost = vipinfo.recharge,
                CostofDay = vipinfo.Dailyspending,
            };
            if (vipinfo.recharge > vipinfo.Dailyspending)
            {
                response.Userstatus = "用户正常";
            }
            else
            {
                response.Userstatus = "用户已欠费";
            }
        }
       
        return Task.FromResult(response);
    }

    public override Task<GetOrdersResponse> GetOrders(GetOrdersRequest request, ServerCallContext context)
    {
        // 1. 查 VIP
        var vip = _dbContext.VIPInfomations
            .FirstOrDefault(v => v.UserId == request.UserId);
        if (vip == null)
            throw new RpcException(new Status(StatusCode.NotFound, "未找到该用户的 VIP 信息"));

        // 2. 拿所有订单，按时间倒序
        var allOrders = _dbContext.Orders
            .Where(o => o.VIPId == vip.Id)
            .OrderByDescending(o => o.OrderDate);

        // 3. 统计总数
        int total = allOrders.Count();

        // 4. 分页
        var paged = allOrders
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // 5. 拼装 List<OrderInfo>
        var infos = new List<OrderInfo>(paged.Count);
        foreach (var order in paged)
        {
            var wa     = _dbContext.Warehouses.Find(order.WarehouseAId);
            var wb     = _dbContext.Warehouses.Find(order.WarehouseBId);
            var driver = _dbContext.Drivers.FirstOrDefault(d=>d.WarehouseDistanceId == order.DistanceId);
            var car    = _dbContext.Cars.FirstOrDefault(c => c.WarehousId == order.WarehouseAId);

            infos.Add(new OrderInfo
            {
                WarehouseAName     = wa?.Name ?? "",
                WarehouseBName     = wb?.Name ?? "",
                Price              = order.Price,
                DriverName         = driver?.Name ?? "",
                DriverPhone        = driver?.PhoneNumber ?? "",
                LicensePlateNumber = car?.LicensePlateNumber ?? "",
                OrderDate          = order.OrderDate.ToUniversalTime().ToTimestamp()
            });
        }

        // 6. 构造返回值
        var response = new GetOrdersResponse
        {
            PageIndex = request.PageIndex,
            PageSize  = request.PageSize,
            Total     = total
        };
        response.Orders.AddRange(infos);

        return Task.FromResult(response);
    }
}