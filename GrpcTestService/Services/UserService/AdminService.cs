using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using My.GRPC.Demo;

namespace GrpcTestService.Services.UserService;

public class AdminService :Admin.AdminBase
{
    private readonly AppDbContext _dbContext;

    public AdminService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public override Task<AdminRegistrationResponse> AdminRegistration(AdminRegistrationRequest request, ServerCallContext context)
    {
        var user = _dbContext.Users.FirstOrDefault(u=>u.Id == request.UserId);
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
        }

        if (request.AdminPassword == "123456")
        {
            user.IsAdmin = true;
        }else if (request.AdminPassword != "123456")
        {
            throw new RpcException(new Status(StatusCode.NotFound, "密码错误"));
        }
        _dbContext.SaveChanges();
        return Task.FromResult( new AdminRegistrationResponse()
        {
            Status = "success",
        });
    }

    public override Task<DailySettlementResponse> DailySettlement(DailySettlementRequest request, ServerCallContext context)
    {
        var vips = _dbContext.VIPInfomations.ToList();
        List<OverdueUser> allOverdueUsers = new();

        foreach (var vip in vips)
        {
            vip.recharge -= vip.Dailyspending;

            if (vip.recharge < 0)
            {
                var user = _dbContext.Users.FirstOrDefault(u => u.Id == vip.UserId);
                if (user != null)
                {
                    allOverdueUsers.Add(new OverdueUser
                    {
                        UserName = user.Name,
                        OverdueAmount = -vip.recharge,
                        Email = user.Email
                    });
                }
            }
        }

        // 保存 recharge 的修改，如果不想落库可移除
        _dbContext.SaveChanges();

        // 分页处理
        int total = allOverdueUsers.Count;
        var pagedUsers = allOverdueUsers
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var response = new DailySettlementResponse
        {
            PageSize = request.PageSize,
            PageIndex = request.PageIndex,
            Total = total
        };
        response.OverdueUser.AddRange(pagedUsers);

        return Task.FromResult(response);
    }

    public override Task<OverdueUserListResponset> OverdueUserList(OverdueUserListRequest request, ServerCallContext context)
    {
        var vips = _dbContext.VIPInfomations.ToList();
        List<OverdueUser> allOverdueUsers = new();

        foreach (var vip in vips)
        {
            if (vip.recharge < 0)
            {
                var user = _dbContext.Users.FirstOrDefault(u => u.Id == vip.UserId);
                if (user == null) continue;

                // 如果请求中包含用户名，进行模糊匹配；否则添加所有
                if (string.IsNullOrEmpty(request.UserName) || user.Name.Contains(request.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    allOverdueUsers.Add(new OverdueUser()
                    {
                        UserName = user.Name,
                        OverdueAmount = -vip.recharge,
                        Email = user.Email
                    });
                }
            }
        }

        // 分页处理
        int total = allOverdueUsers.Count;
        var pagedUsers = allOverdueUsers
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var response = new OverdueUserListResponset
        {
            PageSize = request.PageSize,
            PageIndex = request.PageIndex,
            Total = total
        };
        response.OverdueUser.AddRange(pagedUsers);

        return Task.FromResult(response);
    }


    public override Task<OrderOverviewResponse> OrderOverview(OrderOverviewRequest request, ServerCallContext context)
    {
        var ordersQuery = from o in _dbContext.Orders
            join vip in _dbContext.VIPInfomations on o.VIPId equals vip.Id
            join user in _dbContext.Users on vip.UserId equals user.Id
            join wa in _dbContext.Warehouses on o.WarehouseAId equals wa.Id
            join wb in _dbContext.Warehouses on o.WarehouseBId equals wb.Id
            join d in _dbContext.Drivers on o.DistanceId equals d.WarehouseDistanceId
            join c in _dbContext.Cars on o.WarehouseAId equals c.WarehousId
            where string.IsNullOrEmpty(request.Username) || 
                  EF.Functions.Like(user.Name, $"%{request.Username}%")
            select new
            {
                o.OrderDate,
                o.Price,
                WarehouseAName = wa.Name,
                WarehouseBName = wb.Name,
                DriverName = d.Name,
                DriverPhone = d.PhoneNumber,
                LicensePlateNumber = c.LicensePlateNumber
            };

        int total = ordersQuery.Count();

        var pagedOrders = ordersQuery
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var response = new OrderOverviewResponse
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Total = total
        };

        response.Orders.AddRange(pagedOrders.Select(x => new OrderInfo
        {
            WarehouseAName = x.WarehouseAName,
            WarehouseBName = x.WarehouseBName,
            Price = x.Price,
            DriverName = x.DriverName,
            DriverPhone = x.DriverPhone,
            LicensePlateNumber = x.LicensePlateNumber,
            OrderDate = Timestamp.FromDateTime(x.OrderDate.ToUniversalTime())
        }));

        return Task.FromResult(response);
    }
}