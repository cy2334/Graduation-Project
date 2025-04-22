
using Grpc.Core;
using GrpcTestService.Models;
using My.GRPC.Demo;
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
        if (request.Name == null && request.Address == null)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "输入信息不全"));
        }

        foreach (var wharehouse in _dbContext.Warehouses)
        {
            if (request.Name == wharehouse.Name)
            {
                throw new RpcException(new Status(StatusCode.Unimplemented, "仓库重复"));
            }
        }

        _dbContext.Warehouses.Add(new Models.Warehouse()
        {
            Name = request.Name,
            Address = request.Address,
            Volume = request.Volume
        });
        _dbContext.SaveChanges();
        return Task.FromResult(new WarehouseResponse()
        {
            Status = "添加仓库成功"
        });
    }

    public override Task<WarehouseDistanceReponse> WarehouseDistance(WarehouseDistanceRequest request, ServerCallContext context)
    {
        if (request.WarehouseAName == null && request.WarehouseBName == null&&request.WarehouseAName ==request.WarehouseBName)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "仓库名称有问题"));
        }

        _dbContext.WarehouseDistanceAssociationTables.Add(new WarehouseDistanceAssociationTable()
        {
            WarehouseAName = request.WarehouseAName,
            WarehouseBName = request.WarehouseBName,
            Distance = request.Distance
        });
        _dbContext.SaveChanges();
        return Task.FromResult(new WarehouseDistanceReponse()
        {
            Status = "添加仓库关系成功"
        });
    }

    public override Task<WarehousingofgoodsReponse> Warehousingofgoods(WarehousingofgoodsRequest request, ServerCallContext context)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Name == request.Name);
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "找不到该用户"));
        }

        var warehouse = _dbContext.Warehouses.FirstOrDefault(v => v.Name == request.Name);
        if (warehouse == null)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "找不到对应仓库"));
        }

        var volume = warehouse.Volume;
        foreach (var relation in _dbContext.VIPAndWarehouseRelations)
        {
            if (relation.WarehouseId == warehouse.Id)
            {
                volume -= relation.OccupiesVolume;
            }
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

        _dbContext.VIPAndWarehouseRelations.Add(new VIPAndWarehouseRelations
        {
            WarehouseId = warehouse.Id,
            VIPId = vip.Id,
            OccupiesVolume = request.Volumeofgoods
        });

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
        var graph = new Dictionary<string, List<(string target, int distance)>>();

        // 构建有向图（单向）
        foreach (var edge in _dbContext.WarehouseDistanceAssociationTables)
        {
            if (!graph.ContainsKey(edge.WarehouseAName))
                graph[edge.WarehouseAName] = new List<(string, int)>();

            graph[edge.WarehouseAName].Add((edge.WarehouseBName, edge.Distance));
        }

        // 计算最短路径
        var distance = Dijkstra(graph, request.WarehouseAName, request.WarehouseBName);

        if (distance == int.MaxValue)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "无法从指定仓库到达目标仓库"));
        }

        return Task.FromResult(new WarehouseTransportReponse
        {
            Distance = distance,
            Cost = distance * 2
        });
    }

    public override Task<WarehouseDisplayReponse> WarehouseDisplay(WarehouseDisplayRequest request, ServerCallContext context)
    {
        List<WarehouseInfo> warehouses = new List<WarehouseInfo>();

        foreach (var warehouse in _dbContext.Warehouses)
        {
            int residualvolume = warehouse.Volume;

            foreach (var vipAndWarehouseRelation in _dbContext.VIPAndWarehouseRelations)
            {
                if (vipAndWarehouseRelation.WarehouseId == warehouse.Id)
                {
                    residualvolume -= vipAndWarehouseRelation.OccupiesVolume;
                }
            }

            // ✅ 正确地将 WarehouseInfo 加入到列表中
            warehouses.Add(new WarehouseInfo
            {
                Warehousename = warehouse.Name,
                Totalvolume = warehouse.Volume,
                Residualvolume = residualvolume
            });
        }

        // ✅ 构造响应并返回
        var response = new WarehouseDisplayReponse
        {
            Pageindex = request.Pageindex,
            Pagesize = request.Pagesize,
            Total = warehouses.Count
        };

        response.Warehouseinfos.AddRange(warehouses);

        return Task.FromResult(response);
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

