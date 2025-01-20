using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using My.GRPC.Demo;

namespace GrpcTestService.Services;

public class DemoService :Demo.DemoBase
{
    public override async Task<GetCustomerByIdResponse> GetCustomerById(GetCustomerByIdRequest request, ServerCallContext context)
    {
        //获取请求中的header
        var head = context.GetHttpContext().Request.Headers["x"];

        //写入响应的header
        var metadata = new Metadata() { };
        metadata.Add("y", "666");
        await context.WriteResponseHeadersAsync(metadata);
    
        //通过解析后的token获取当前登陆人相关信息
        var claims = context.GetHttpContext().User.Claims.ToArray();
        var res = new GetCustomerByIdResponse();
        res.Createtime = Timestamp.FromDateTime(DateTime.UtcNow);
        return await Task.FromResult(res);
    }
}