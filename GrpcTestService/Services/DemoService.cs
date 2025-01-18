using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using My.GRPC.Demo;

namespace GrpcTestService.Services;

public class DemoService :Demo.DemoBase
{
    public override Task<GetCustomerByIdResponse> GetCustomerById(GetCustomerByIdRequest request, ServerCallContext context)
    {
        var res = new GetCustomerByIdResponse();
        res.Createtime = Timestamp.FromDateTime(DateTime.UtcNow);
        return Task.FromResult(res);
    }
}