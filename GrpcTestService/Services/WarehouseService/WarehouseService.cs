
using Grpc.Core;
using GrpcTestService.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using My.GRPC.Demo;
using Driver = GrpcTestService.Models.Driver;
using Warehouse = My.GRPC.Demo.Warehouse;

namespace GrpcTestService.Services.WarehouseService;

public class WarehouseService :Warehouse.WarehouseBase
{
    private readonly AppDbContext _dbContext;

    public WarehouseService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override Task<WarehouseResponse> WarehouseRegister(WarehouseRequest request, ServerCallContext context)
    {
        // 1. 校验输入
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.LicensePlateNumber)
            || request.LoadingCapacity <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "输入信息不全"));
        }

        // 2. 重复检查
        if (_dbContext.Warehouses.Any(w => w.Name == request.Name))
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "仓库重复"));
        }
        // 3. 使用事务
        using var transaction = _dbContext.Database.BeginTransaction();

        // 添加仓库
        var warehouse = new Models.Warehouse()
        {
            Name    = request.Name,
            Address = request.Address,
            Volume  = request.Volume
        };
        _dbContext.Warehouses.Add(warehouse);
        _dbContext.SaveChanges(); 
        // 添加车辆
        var car = new Models.Car()
        {
            LicensePlateNumber = request.LicensePlateNumber,
            LoadingCapacity    = request.LoadingCapacity,
            WarehousId        = warehouse.Id
        };
        _dbContext.Cars.Add(car);

        // 一次性提交
        _dbContext.SaveChanges();
        transaction.Commit();

        return Task.FromResult(new WarehouseResponse
        {
            Status = "添加仓库成功"
        });
    }

    public override async Task<WarehouseDistanceReponse> WarehouseDistance(WarehouseDistanceRequest request, ServerCallContext context)
    {
        // 1. 校验输入
        if (string.IsNullOrWhiteSpace(request.WarehouseAName)
            || string.IsNullOrWhiteSpace(request.WarehouseBName)
            || request.WarehouseAName == request.WarehouseBName
            || string.IsNullOrWhiteSpace(request.DriverName)
            || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "仓库名称或司机信息有问题"));
        }

        // 2. 使用事务，保证两个表的数据一致性
        using var tx = await _dbContext.Database.BeginTransactionAsync();

        // 3. 插入仓库关系
        var relation = new WarehouseDistanceAssociationTable
        {
            WarehouseAName = request.WarehouseAName,
            WarehouseBName = request.WarehouseBName,
            Distance       = request.Distance
        };
        _dbContext.WarehouseDistanceAssociationTables.Add(relation);
        await _dbContext.SaveChangesAsync();

        // 4. 插入 Driver，并关联上面新增的 relation.Id
        var driver = new Driver
        {
            Name                 = request.DriverName,
            PhoneNumber          = request.PhoneNumber,
            WarehouseDistanceId  = relation.Id
        };
        _dbContext.Drivers.Add(driver);
        await _dbContext.SaveChangesAsync();

        // 5. 提交事务
        await tx.CommitAsync();

        return new WarehouseDistanceReponse
        {
            Status = "添加仓库关系并关联司机成功"
        };
    }

    public override Task<WarehousingofgoodsReponse> Warehousingofgoods(WarehousingofgoodsRequest request, ServerCallContext context)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Name == request.Name);
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "找不到该用户"));
        }

        var warehouse = _dbContext.Warehouses.FirstOrDefault(v => v.Name == request.WarehouseName);
        if (warehouse == null)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "找不到对应仓库"));
        }

        var volume = warehouse.Volume;

        // 计算仓库当前剩余容量
        foreach (var relation in _dbContext.VIPAndWarehouseRelations.Where(r => r.WarehouseId == warehouse.Id))
        {
            volume -= relation.OccupiesVolume;
        }

        if (volume < request.Volumeofgoods)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "仓库无法存入这么多货品"));
        }

        var vip = _dbContext.VIPInfomations.FirstOrDefault(v => v.UserId == user.Id);
        if (vip == null)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "请先注册VIP"));
        }

        int cost = request.Volumeofgoods * 2;

        var existingRelation = _dbContext.VIPAndWarehouseRelations
            .FirstOrDefault(r => r.WarehouseId == warehouse.Id && r.VIPId == vip.Id);

        if (existingRelation == null)
        {
            // 如果没有记录，就新增
            _dbContext.VIPAndWarehouseRelations.Add(new VIPAndWarehouseRelations
            {
                WarehouseId = warehouse.Id,
                VIPId = vip.Id,
                OccupiesVolume = request.Volumeofgoods
            });
        }
        else
        {
            // 如果已有记录，就累加体积
            existingRelation.OccupiesVolume += request.Volumeofgoods;
        }

        // 增加每日消费
        vip.Dailyspending += cost;

        _dbContext.SaveChanges();

        return Task.FromResult(new WarehousingofgoodsReponse
        {
            Remainingvolumeofthewarehouse = (volume - request.Volumeofgoods).ToString(),
            CostofDay = cost.ToString()
        });
    }

    public override Task<WarehouseTransportReponse> WarehouseTransport(WarehouseTransportRequest request, ServerCallContext context) 
{
   // 1. 用户 & VIP 校验
    var user = _dbContext.Users.FirstOrDefault(u => u.Name == request.Name);
    if (user == null)
        throw new RpcException(new Status(StatusCode.NotFound, "用户不存在"));

    var vip = _dbContext.VIPInfomations.FirstOrDefault(v => v.UserId == user.Id);
    if (vip == null)
        throw new RpcException(new Status(StatusCode.NotFound, "VIP 才能下单"));

    // 2. 仓库 A、B 校验
    var warehouseA = _dbContext.Warehouses.FirstOrDefault(w => w.Name == request.WarehouseAName);
    var warehouseB = _dbContext.Warehouses.FirstOrDefault(w => w.Name == request.WarehouseBName);
    if (warehouseA == null || warehouseB == null)
        throw new RpcException(new Status(StatusCode.NotFound, "仓库A或仓库B不存在"));

    // 3. 校验 A 仓库中是否有足够货物
    var relationA = _dbContext.VIPAndWarehouseRelations
        .FirstOrDefault(r => r.WarehouseId == warehouseA.Id && r.VIPId == vip.Id);
    if (relationA == null || relationA.OccupiesVolume < request.Volume)
        throw new RpcException(new Status(StatusCode.FailedPrecondition, "仓库A中无足够货物"));

    // 4. 计算 B 仓库当前剩余容量
    int usedB = _dbContext.VIPAndWarehouseRelations
        .Where(r => r.WarehouseId == warehouseB.Id)
        .Sum(r => r.OccupiesVolume);
    int freeB = warehouseB.Volume - usedB;
    if (freeB < request.Volume)
        throw new RpcException(new Status(StatusCode.FailedPrecondition, "仓库B无法存入这么多货物"));

    // 5. 在 B 仓库插入或累加货物
    var relationB = _dbContext.VIPAndWarehouseRelations
        .FirstOrDefault(r => r.WarehouseId == warehouseB.Id && r.VIPId == vip.Id);
    if (relationB == null)
    {
        _dbContext.VIPAndWarehouseRelations.Add(new VIPAndWarehouseRelations
        {
            WarehouseId   = warehouseB.Id,
            VIPId         = vip.Id,
            OccupiesVolume = request.Volume
        });
    }
    else
    {
        relationB.OccupiesVolume += request.Volume;
    }

    // 6. 从 A 仓库扣减货物
    relationA.OccupiesVolume -= request.Volume;

    // 7. 构建有向图并计算最短路径
    var graph = new Dictionary<string, List<(string target, int distance)>>();
    foreach (var edge in _dbContext.WarehouseDistanceAssociationTables)
    {
        if (!graph.ContainsKey(edge.WarehouseAName))
            graph[edge.WarehouseAName] = new List<(string, int)>();
        graph[edge.WarehouseAName].Add((edge.WarehouseBName, edge.Distance));
    }
    int distance = Dijkstra(graph, request.WarehouseAName, request.WarehouseBName);
    if (distance == int.MaxValue)
        throw new RpcException(new Status(StatusCode.NotFound, "无法从指定仓库到达目标仓库"));

    // 8. 计算费用 & 更新 VIP 日消费
    int cost = distance * 2;
    vip.recharge -= cost;

    // 9. 查找这条 A→B 距离对应的表记录，用于关联 Order
    var distanceEntry = _dbContext.WarehouseDistanceAssociationTables
        .First(e => e.WarehouseAName == request.WarehouseAName 
                 && e.WarehouseBName == request.WarehouseBName);

    // 10. 记录一条订单
    _dbContext.Orders.Add(new Order
    {
        VIPId          = vip.Id,
        WarehouseAId   = warehouseA.Id,
        WarehouseBId   = warehouseB.Id,
        Price          = cost,
        OrderDate      = DateTime.UtcNow,
        OrderDetail    = $"{warehouseA.Name}→{warehouseB.Name}",
        DistanceId     = distanceEntry.Id
    });

    // 11. 一次性保存所有变更
    _dbContext.SaveChanges();

    // 12. 返回 gRPC 响应
    return Task.FromResult(new WarehouseTransportReponse
    {
        Distance = distance,
        Cost     = cost
    });
}

    public override Task<WarehouseDisplayReponse> WarehouseDisplay(WarehouseDisplayRequest request, ServerCallContext context)
    {
        // 把数据提前拉出来，避免在遍历中再次访问数据库
        var warehouseList = _dbContext.Warehouses.ToList();
        var relationList = _dbContext.VIPAndWarehouseRelations.ToList();

        List<WarehouseInfo> warehouses = new();

        foreach (var warehouse in warehouseList)
        {
            int residualvolume = warehouse.Volume;

            foreach (var vipAndWarehouseRelation in relationList)
            {
                if (vipAndWarehouseRelation.WarehouseId == warehouse.Id)
                {
                    residualvolume -= vipAndWarehouseRelation.OccupiesVolume;
                }
            }

            warehouses.Add(new WarehouseInfo
            {
                Warehousename = warehouse.Name,
                Totalvolume = warehouse.Volume,
                Residualvolume = residualvolume
            });
        }

        // 分页处理
        int pageIndex = request.Pageindex > 0 ? request.Pageindex : 1;
        int pageSize = request.Pagesize > 0 ? request.Pagesize : 10;
        int skip = (pageIndex - 1) * pageSize;

        var pagedWarehouses = warehouses
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        var response = new WarehouseDisplayReponse
        {
            Pageindex = pageIndex,
            Pagesize = pageSize,
            Total = warehouses.Count
        };

        response.Warehouseinfos.AddRange(pagedWarehouses);

        return Task.FromResult(response);
    }

    public override Task<GoodsOutboundResponse> GoodsOutbound(GoodsOutboundRequest request, ServerCallContext context)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Name == request.Name);
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "找不到该用户"));
        }

        var warehouse = _dbContext.Warehouses.FirstOrDefault(w => w.Name == request.WarehouseName);
        if (warehouse == null)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "找不到对应仓库"));
        }

        var vip = _dbContext.VIPInfomations.FirstOrDefault(v => v.UserId == user.Id);
        if (vip == null)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "请先注册VIP"));
        }

        var relation = _dbContext.VIPAndWarehouseRelations
            .FirstOrDefault(r => r.WarehouseId == warehouse.Id && r.VIPId == vip.Id);

        if (relation == null || relation.OccupiesVolume < request.Volumeofgoods)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "用户在该仓库中没有足够的货物可以出仓"));
        }

        // 扣除体积
        relation.OccupiesVolume -= request.Volumeofgoods;
        
         vip.Dailyspending -= (request.Volumeofgoods * 2);

        _dbContext.SaveChanges();

        // 重新计算仓库剩余容量
        var remainingVolume = warehouse.Volume;
        foreach (var r in _dbContext.VIPAndWarehouseRelations.Where(r => r.WarehouseId == warehouse.Id))
        {
            remainingVolume -= r.OccupiesVolume;
        }

        return Task.FromResult(new GoodsOutboundResponse
        {
            RemainingGoods = remainingVolume,
            Status = "出仓成功"
        });
    }

    private int Dijkstra(Dictionary<string, List<(string target, int distance)>> graph, string start, string end)
    {
        var distances = new Dictionary<string, int>();
        var visited = new HashSet<string>();
        var queue = new PriorityQueue<string, int>();

        foreach (var node in graph.Keys)
            distances[node] = int.MaxValue;

        distances[start] = 0;
        queue.Enqueue(start, 0);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current))
                continue;

            visited.Add(current);

            if (!graph.ContainsKey(current))
                continue;

            foreach (var (neighbor, cost) in graph[current])
            {
                var newDist = distances[current] + cost;
                if (!distances.ContainsKey(neighbor) || newDist < distances[neighbor])
                {
                    distances[neighbor] = newDist;
                    queue.Enqueue(neighbor, newDist);
                }
            }
        }

        return distances.ContainsKey(end) ? distances[end] : int.MaxValue;
    }
}

