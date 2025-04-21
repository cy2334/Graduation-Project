
using Grpc.Core;
using My.GRPC.Demo;

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
        if (request.Name == null && request.Address == null && request.Capacity == null)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "输入信息不全"));
        }
        
        return base.WarehouseRegister(request, context);
    }
}
