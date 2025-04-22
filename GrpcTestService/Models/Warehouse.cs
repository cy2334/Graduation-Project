namespace GrpcTestService.Models;
public class Warehouse
{
    public int Id { get; set; }
    public string Address { get; set; }  // 建议属性名首字母大写
    public int Volume { get; set; }      // 类型改为 int，并重命名
    public string Name { get; set; }
}
