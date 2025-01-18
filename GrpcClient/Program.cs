using My.GRPC.Demo;
using Grpc.Net.Client;

using var channel = GrpcChannel.ForAddress("http://localhost:5069");
var client = new Demo.DemoClient(channel);
var res =  client.GetCustomerById(new GetCustomerByIdRequest() { Id = 1 });
Console.WriteLine(res.Createtime);
Console.ReadKey();